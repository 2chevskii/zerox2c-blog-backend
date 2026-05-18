using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZeroX2C.Blog.API.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPostSearchRoutines : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS SearchPublishedPostIds;");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS SearchAdminPostIds;");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS PostSearchIsLooseMatch;");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS PostSearchLevenshteinDistance;");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS NormalizePostSearchBooleanQuery;");

            migrationBuilder.Sql(
                """
                CREATE FUNCTION NormalizePostSearchBooleanQuery(search_text varchar(512))
                RETURNS varchar(2048)
                DETERMINISTIC
                NO SQL
                BEGIN
                    DECLARE term varchar(128);
                    DECLARE occurrence_index int DEFAULT 1;
                    DECLARE result varchar(2048) DEFAULT '';

                    IF search_text IS NULL OR TRIM(search_text) = '' THEN
                        RETURN NULL;
                    END IF;

                    read_loop: LOOP
                        SET term = REGEXP_SUBSTR(LOWER(search_text), '[[:alnum:]]+', 1, occurrence_index);

                        IF term IS NULL THEN
                            LEAVE read_loop;
                        END IF;

                        IF LOCATE(CONCAT(term, '*'), result) = 0 THEN
                            SET result = CONCAT_WS(' ', NULLIF(result, ''), CONCAT(term, '*'));
                        END IF;

                        SET occurrence_index = occurrence_index + 1;
                    END LOOP;

                    RETURN NULLIF(result, '');
                END
                """
            );

            migrationBuilder.Sql(
                """
                CREATE FUNCTION PostSearchLevenshteinDistance(
                    left_value varchar(128),
                    right_value varchar(128),
                    max_distance int
                )
                RETURNS int
                DETERMINISTIC
                NO SQL
                BEGIN
                    DECLARE left_length int;
                    DECLARE right_length int;
                    DECLARE row_index int DEFAULT 1;
                    DECLARE column_index int;
                    DECLARE edit_cost int;
                    DECLARE insertion_cost int;
                    DECLARE deletion_cost int;
                    DECLARE substitution_cost int;
                    DECLARE current_value int;
                    DECLARE row_minimum int;
                    DECLARE previous_previous_row json DEFAULT JSON_ARRAY();
                    DECLARE previous_row json DEFAULT JSON_ARRAY();
                    DECLARE current_row json DEFAULT JSON_ARRAY();

                    IF left_value IS NULL OR right_value IS NULL THEN
                        RETURN max_distance + 1;
                    END IF;

                    SET left_value = LOWER(LEFT(left_value, 128));
                    SET right_value = LOWER(LEFT(right_value, 128));
                    SET left_length = CHAR_LENGTH(left_value);
                    SET right_length = CHAR_LENGTH(right_value);
                    SET max_distance = GREATEST(IFNULL(max_distance, 0), 0);

                    IF left_value = right_value THEN
                        RETURN 0;
                    END IF;

                    IF ABS(left_length - right_length) > max_distance THEN
                        RETURN max_distance + 1;
                    END IF;

                    IF left_length = 0 THEN
                        RETURN right_length;
                    END IF;

                    IF right_length = 0 THEN
                        RETURN left_length;
                    END IF;

                    SET column_index = 0;
                    WHILE column_index <= right_length DO
                        SET previous_row = JSON_ARRAY_APPEND(previous_row, '$', column_index);
                        SET column_index = column_index + 1;
                    END WHILE;

                    WHILE row_index <= left_length DO
                        SET current_row = JSON_ARRAY(row_index);
                        SET row_minimum = row_index;
                        SET column_index = 1;

                        WHILE column_index <= right_length DO
                            SET edit_cost = IF(
                                SUBSTRING(left_value, row_index, 1) = SUBSTRING(right_value, column_index, 1),
                                0,
                                1
                            );
                            SET insertion_cost = CAST(JSON_UNQUOTE(JSON_EXTRACT(current_row, CONCAT('$[', column_index - 1, ']'))) AS UNSIGNED) + 1;
                            SET deletion_cost = CAST(JSON_UNQUOTE(JSON_EXTRACT(previous_row, CONCAT('$[', column_index, ']'))) AS UNSIGNED) + 1;
                            SET substitution_cost = CAST(JSON_UNQUOTE(JSON_EXTRACT(previous_row, CONCAT('$[', column_index - 1, ']'))) AS UNSIGNED) + edit_cost;
                            SET current_value = LEAST(insertion_cost, deletion_cost, substitution_cost);

                            IF row_index > 1
                                AND column_index > 1
                                AND SUBSTRING(left_value, row_index, 1) = SUBSTRING(right_value, column_index - 1, 1)
                                AND SUBSTRING(left_value, row_index - 1, 1) = SUBSTRING(right_value, column_index, 1)
                            THEN
                                SET current_value = LEAST(
                                    current_value,
                                    CAST(JSON_UNQUOTE(JSON_EXTRACT(previous_previous_row, CONCAT('$[', column_index - 2, ']'))) AS UNSIGNED) + 1
                                );
                            END IF;

                            SET current_row = JSON_ARRAY_APPEND(current_row, '$', current_value);
                            SET row_minimum = LEAST(row_minimum, current_value);
                            SET column_index = column_index + 1;
                        END WHILE;

                        IF row_minimum > max_distance THEN
                            RETURN max_distance + 1;
                        END IF;

                        SET previous_previous_row = previous_row;
                        SET previous_row = current_row;
                        SET row_index = row_index + 1;
                    END WHILE;

                    RETURN CAST(JSON_UNQUOTE(JSON_EXTRACT(previous_row, CONCAT('$[', right_length, ']'))) AS UNSIGNED);
                END
                """
            );

            migrationBuilder.Sql(
                """
                CREATE FUNCTION PostSearchIsLooseMatch(search_text longtext, query_text varchar(512))
                RETURNS boolean
                DETERMINISTIC
                NO SQL
                BEGIN
                    DECLARE term varchar(128);
                    DECLARE token varchar(128);
                    DECLARE term_occurrence int DEFAULT 1;
                    DECLARE token_occurrence int;
                    DECLARE max_distance int;
                    DECLARE normalized_text longtext;
                    DECLARE normalized_query varchar(512);

                    IF search_text IS NULL OR query_text IS NULL OR TRIM(query_text) = '' THEN
                        RETURN false;
                    END IF;

                    SET normalized_text = LOWER(search_text);
                    SET normalized_query = LOWER(query_text);

                    term_loop: LOOP
                        SET term = REGEXP_SUBSTR(normalized_query, '[[:alnum:]]+', 1, term_occurrence);

                        IF term IS NULL THEN
                            LEAVE term_loop;
                        END IF;

                        IF CHAR_LENGTH(term) < 3 THEN
                            IF REGEXP_LIKE(normalized_text, CONCAT('(^|[^[:alnum:]])', term, '([^[:alnum:]]|$)')) THEN
                                RETURN true;
                            END IF;
                        ELSEIF normalized_text LIKE CONCAT('%', term, '%') THEN
                            RETURN true;
                        END IF;

                        SET max_distance = CASE
                            WHEN CHAR_LENGTH(term) < 4 THEN 0
                            WHEN CHAR_LENGTH(term) <= 7 THEN 1
                            ELSE 2
                        END;

                        IF max_distance > 0 THEN
                            SET token_occurrence = 1;

                            token_loop: LOOP
                                SET token = REGEXP_SUBSTR(normalized_text, '[[:alnum:]]+', 1, token_occurrence);

                                IF token IS NULL THEN
                                    LEAVE token_loop;
                                END IF;

                                IF ABS(CHAR_LENGTH(token) - CHAR_LENGTH(term)) <= max_distance
                                    AND PostSearchLevenshteinDistance(token, term, max_distance) <= max_distance
                                THEN
                                    RETURN true;
                                END IF;

                                SET token_occurrence = token_occurrence + 1;
                            END LOOP;
                        END IF;

                        SET term_occurrence = term_occurrence + 1;
                    END LOOP;

                    RETURN false;
                END
                """
            );

            migrationBuilder.Sql(
                """
                CREATE PROCEDURE SearchPublishedPostIds(
                    IN search_text varchar(512),
                    IN tag_names varchar(512),
                    IN published_from datetime(6),
                    IN published_to datetime(6),
                    IN page_offset int,
                    IN page_limit int
                )
                BEGIN
                    DECLARE full_text_query varchar(2048);
                    DECLARE required_tag_count int DEFAULT 0;
                    DECLARE candidate_limit int DEFAULT 500;

                    SET page_offset = GREATEST(IFNULL(page_offset, 0), 0);
                    SET page_limit = LEAST(GREATEST(IFNULL(page_limit, 10), 1), 100);
                    SET candidate_limit = LEAST(500, GREATEST((page_offset + page_limit) * 10, 100));
                    SET full_text_query = NormalizePostSearchBooleanQuery(search_text);
                    SET tag_names = NULLIF(TRIM(BOTH ',' FROM IFNULL(tag_names, '')), '');

                    IF tag_names IS NOT NULL THEN
                        SET required_tag_count = 1 + CHAR_LENGTH(tag_names) - CHAR_LENGTH(REPLACE(tag_names, ',', ''));
                    END IF;

                    DROP TEMPORARY TABLE IF EXISTS PostSearchResults;
                    CREATE TEMPORARY TABLE PostSearchResults
                    (
                        PostId char(36) NOT NULL PRIMARY KEY,
                        SortDate datetime(6) NOT NULL
                    ) ENGINE=MEMORY;

                    INSERT IGNORE INTO PostSearchResults (PostId, SortDate)
                    SELECT p.Id, p.PublishedAt
                    FROM Posts p
                    INNER JOIN PostMarkdownDocuments d ON d.PostId = p.Id
                    WHERE full_text_query IS NOT NULL
                        AND p.IsDeleted = false
                        AND p.Status = 'Published'
                        AND p.PublishedAt IS NOT NULL
                        AND d.IsDeleted = false
                        AND (published_from IS NULL OR p.PublishedAt >= published_from)
                        AND (published_to IS NULL OR p.PublishedAt < DATE_ADD(published_to, INTERVAL 1 DAY))
                        AND (
                            required_tag_count = 0
                            OR (
                                SELECT COUNT(DISTINCT t.Name)
                                FROM PostTags pt
                                INNER JOIN Tags t ON t.Id = pt.TagId
                                WHERE pt.PostId = p.Id
                                    AND pt.IsDeleted = false
                                    AND t.IsDeleted = false
                                    AND FIND_IN_SET(t.Name, tag_names) > 0
                            ) = required_tag_count
                        )
                        AND (
                            MATCH(p.Title, p.Subtitle, p.Slug) AGAINST (full_text_query IN BOOLEAN MODE)
                            OR MATCH(d.PlainText) AGAINST (full_text_query IN BOOLEAN MODE)
                            OR EXISTS (
                                SELECT 1
                                FROM PostTags pt
                                INNER JOIN Tags t ON t.Id = pt.TagId
                                WHERE pt.PostId = p.Id
                                    AND pt.IsDeleted = false
                                    AND t.IsDeleted = false
                                    AND MATCH(t.Name, t.Description) AGAINST (full_text_query IN BOOLEAN MODE)
                            )
                        );

                    INSERT IGNORE INTO PostSearchResults (PostId, SortDate)
                    SELECT candidate.PostId, candidate.SortDate
                    FROM (
                        SELECT
                            p.Id AS PostId,
                            p.PublishedAt AS SortDate,
                            CONCAT_WS(
                                ' ',
                                p.Title,
                                p.Subtitle,
                                p.Slug,
                                d.PlainText,
                                (
                                    SELECT GROUP_CONCAT(CONCAT_WS(' ', t.Name, t.Description) SEPARATOR ' ')
                                    FROM PostTags pt
                                    INNER JOIN Tags t ON t.Id = pt.TagId
                                    WHERE pt.PostId = p.Id
                                        AND pt.IsDeleted = false
                                        AND t.IsDeleted = false
                                )
                            ) AS SearchText
                        FROM Posts p
                        INNER JOIN PostMarkdownDocuments d ON d.PostId = p.Id
                        WHERE p.IsDeleted = false
                            AND p.Status = 'Published'
                            AND p.PublishedAt IS NOT NULL
                            AND d.IsDeleted = false
                            AND (published_from IS NULL OR p.PublishedAt >= published_from)
                            AND (published_to IS NULL OR p.PublishedAt < DATE_ADD(published_to, INTERVAL 1 DAY))
                            AND (
                                required_tag_count = 0
                                OR (
                                    SELECT COUNT(DISTINCT t.Name)
                                    FROM PostTags pt
                                    INNER JOIN Tags t ON t.Id = pt.TagId
                                    WHERE pt.PostId = p.Id
                                        AND pt.IsDeleted = false
                                        AND t.IsDeleted = false
                                        AND FIND_IN_SET(t.Name, tag_names) > 0
                                ) = required_tag_count
                            )
                        ORDER BY p.PublishedAt DESC, p.Id DESC
                        LIMIT candidate_limit
                    ) candidate
                    WHERE PostSearchIsLooseMatch(candidate.SearchText, search_text);

                    SELECT PostId
                    FROM PostSearchResults
                    ORDER BY SortDate DESC, PostId DESC
                    LIMIT page_offset, page_limit;

                    DROP TEMPORARY TABLE IF EXISTS PostSearchResults;
                END
                """
            );

            migrationBuilder.Sql(
                """
                CREATE PROCEDURE SearchAdminPostIds(
                    IN search_text varchar(512),
                    IN post_status varchar(32),
                    IN page_offset int,
                    IN page_limit int
                )
                BEGIN
                    DECLARE full_text_query varchar(2048);
                    DECLARE candidate_limit int DEFAULT 500;

                    SET page_offset = GREATEST(IFNULL(page_offset, 0), 0);
                    SET page_limit = LEAST(GREATEST(IFNULL(page_limit, 10), 1), 100);
                    SET candidate_limit = LEAST(500, GREATEST((page_offset + page_limit) * 10, 100));
                    SET full_text_query = NormalizePostSearchBooleanQuery(search_text);
                    SET post_status = NULLIF(TRIM(IFNULL(post_status, '')), '');

                    DROP TEMPORARY TABLE IF EXISTS PostSearchResults;
                    CREATE TEMPORARY TABLE PostSearchResults
                    (
                        PostId char(36) NOT NULL PRIMARY KEY,
                        SortDate datetime(6) NOT NULL
                    ) ENGINE=MEMORY;

                    INSERT IGNORE INTO PostSearchResults (PostId, SortDate)
                    SELECT p.Id, p.CreatedAt
                    FROM Posts p
                    LEFT JOIN PostMarkdownDrafts d ON d.PostId = p.Id AND d.IsDeleted = false
                    WHERE full_text_query IS NOT NULL
                        AND p.IsDeleted = false
                        AND (post_status IS NULL OR p.Status = post_status)
                        AND (
                            MATCH(p.Title, p.Subtitle, p.Slug) AGAINST (full_text_query IN BOOLEAN MODE)
                            OR (
                                d.Id IS NOT NULL
                                AND MATCH(d.PlainText) AGAINST (full_text_query IN BOOLEAN MODE)
                            )
                        );

                    INSERT IGNORE INTO PostSearchResults (PostId, SortDate)
                    SELECT candidate.PostId, candidate.SortDate
                    FROM (
                        SELECT
                            p.Id AS PostId,
                            p.CreatedAt AS SortDate,
                            CONCAT_WS(' ', p.Title, p.Subtitle, p.Slug, d.PlainText) AS SearchText
                        FROM Posts p
                        LEFT JOIN PostMarkdownDrafts d ON d.PostId = p.Id AND d.IsDeleted = false
                        WHERE p.IsDeleted = false
                            AND (post_status IS NULL OR p.Status = post_status)
                        ORDER BY p.CreatedAt DESC, p.Id DESC
                        LIMIT candidate_limit
                    ) candidate
                    WHERE PostSearchIsLooseMatch(candidate.SearchText, search_text);

                    SELECT PostId
                    FROM PostSearchResults
                    ORDER BY SortDate DESC, PostId DESC
                    LIMIT page_offset, page_limit;

                    DROP TEMPORARY TABLE IF EXISTS PostSearchResults;
                END
                """
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS SearchPublishedPostIds;");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS SearchAdminPostIds;");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS PostSearchIsLooseMatch;");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS PostSearchLevenshteinDistance;");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS NormalizePostSearchBooleanQuery;");
        }
    }
}
