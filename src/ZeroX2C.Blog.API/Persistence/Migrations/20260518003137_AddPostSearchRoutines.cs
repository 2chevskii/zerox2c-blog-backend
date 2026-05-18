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
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS ApplyLoosePostSearchMatches;");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS PostSearchIsLooseMatch;");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS PostSearchLevenshteinDistance;");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS NormalizePostSearchBooleanQuery;");

            migrationBuilder.Sql(
                """
                CREATE PROCEDURE ApplyLoosePostSearchMatches()
                SQL SECURITY INVOKER
                BEGIN
                    DECLARE done boolean DEFAULT false;
                    DECLARE candidate_post_id char(36);
                    DECLARE candidate_sort_date datetime(6);
                    DECLARE candidate_text longtext;
                    DECLARE token varchar(128);
                    DECLARE token_occurrence int;
                    DECLARE pair_post_id char(36);
                    DECLARE pair_sort_date datetime(6);
                    DECLARE pair_token varchar(128);
                    DECLARE pair_term varchar(128);
                    DECLARE pair_max_distance int;
                    DECLARE left_value varchar(128);
                    DECLARE right_value varchar(128);
                    DECLARE left_length int;
                    DECLARE right_length int;
                    DECLARE row_index int;
                    DECLARE column_index int;
                    DECLARE edit_cost int;
                    DECLARE insertion_cost int;
                    DECLARE deletion_cost int;
                    DECLARE substitution_cost int;
                    DECLARE current_value int;
                    DECLARE row_minimum int;
                    DECLARE distance int;
                    DECLARE previous_previous_row json;
                    DECLARE previous_row json;
                    DECLARE current_row json;

                    DECLARE candidate_cursor CURSOR FOR
                        SELECT PostId, SortDate, SearchText
                        FROM PostSearchCandidates;

                    DECLARE pair_cursor CURSOR FOR
                        SELECT PostId, SortDate, Token, Term, MaxDistance
                        FROM PostSearchPairs;

                    DECLARE CONTINUE HANDLER FOR NOT FOUND SET done = true;

                    DROP TEMPORARY TABLE IF EXISTS PostSearchTokens;
                    CREATE TEMPORARY TABLE PostSearchTokens
                    (
                        PostId char(36) NOT NULL,
                        SortDate datetime(6) NOT NULL,
                        Token varchar(128) NOT NULL,
                        UNIQUE KEY IX_PostSearchTokens_PostId_Token (PostId, Token),
                        KEY IX_PostSearchTokens_Token (Token)
                    ) ENGINE=MEMORY;

                    SET done = false;
                    OPEN candidate_cursor;

                    candidate_loop: LOOP
                        FETCH candidate_cursor INTO candidate_post_id, candidate_sort_date, candidate_text;

                        IF done THEN
                            LEAVE candidate_loop;
                        END IF;

                        SET token_occurrence = 1;

                        token_loop: LOOP
                            SET token = REGEXP_SUBSTR(LOWER(candidate_text), '[[:alnum:]]+', 1, token_occurrence);

                            IF token IS NULL THEN
                                LEAVE token_loop;
                            END IF;

                            INSERT IGNORE INTO PostSearchTokens (PostId, SortDate, Token)
                            VALUES (candidate_post_id, candidate_sort_date, LEFT(token, 128));

                            SET token_occurrence = token_occurrence + 1;
                        END LOOP;
                    END LOOP;

                    CLOSE candidate_cursor;

                    INSERT IGNORE INTO PostSearchResults (PostId, SortDate)
                    SELECT DISTINCT tokens.PostId, tokens.SortDate
                    FROM PostSearchTokens tokens
                    INNER JOIN PostSearchTerms terms ON (
                        (CHAR_LENGTH(terms.Term) < 3 AND tokens.Token = terms.Term)
                        OR (CHAR_LENGTH(terms.Term) >= 3 AND tokens.Token LIKE CONCAT('%', terms.Term, '%'))
                    );

                    DROP TEMPORARY TABLE IF EXISTS PostSearchPairs;
                    CREATE TEMPORARY TABLE PostSearchPairs
                    (
                        PostId char(36) NOT NULL,
                        SortDate datetime(6) NOT NULL,
                        Token varchar(128) NOT NULL,
                        Term varchar(128) NOT NULL,
                        MaxDistance int NOT NULL,
                        KEY IX_PostSearchPairs_PostId (PostId)
                    ) ENGINE=MEMORY;

                    INSERT INTO PostSearchPairs (PostId, SortDate, Token, Term, MaxDistance)
                    SELECT tokens.PostId, tokens.SortDate, tokens.Token, terms.Term, terms.MaxDistance
                    FROM PostSearchTokens tokens
                    INNER JOIN PostSearchTerms terms ON terms.MaxDistance > 0
                    WHERE ABS(CHAR_LENGTH(tokens.Token) - CHAR_LENGTH(terms.Term)) <= terms.MaxDistance;

                    SET done = false;
                    OPEN pair_cursor;

                    pair_loop: LOOP
                        FETCH pair_cursor INTO pair_post_id, pair_sort_date, pair_token, pair_term, pair_max_distance;

                        IF done THEN
                            LEAVE pair_loop;
                        END IF;

                        SET left_value = LOWER(LEFT(pair_token, 128));
                        SET right_value = LOWER(LEFT(pair_term, 128));
                        SET left_length = CHAR_LENGTH(left_value);
                        SET right_length = CHAR_LENGTH(right_value);
                        SET distance = pair_max_distance + 1;

                        IF left_value = right_value THEN
                            SET distance = 0;
                        ELSEIF ABS(left_length - right_length) <= pair_max_distance THEN
                            SET previous_previous_row = JSON_ARRAY();
                            SET previous_row = JSON_ARRAY();
                            SET column_index = 0;

                            WHILE column_index <= right_length DO
                                SET previous_row = JSON_ARRAY_APPEND(previous_row, '$', column_index);
                                SET column_index = column_index + 1;
                            END WHILE;

                            SET row_index = 1;

                            distance_loop: WHILE row_index <= left_length DO
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

                                IF row_minimum > pair_max_distance THEN
                                    SET distance = pair_max_distance + 1;
                                    SET row_index = left_length + 1;
                                ELSE
                                    SET distance = CAST(JSON_UNQUOTE(JSON_EXTRACT(current_row, CONCAT('$[', right_length, ']'))) AS UNSIGNED);
                                    SET previous_previous_row = previous_row;
                                    SET previous_row = current_row;
                                    SET row_index = row_index + 1;
                                END IF;
                            END WHILE;
                        END IF;

                        IF distance <= pair_max_distance THEN
                            INSERT IGNORE INTO PostSearchResults (PostId, SortDate)
                            VALUES (pair_post_id, pair_sort_date);
                        END IF;
                    END LOOP;

                    CLOSE pair_cursor;

                    DROP TEMPORARY TABLE IF EXISTS PostSearchPairs;
                    DROP TEMPORARY TABLE IF EXISTS PostSearchTokens;
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
                SQL SECURITY INVOKER
                BEGIN
                    DECLARE full_text_query varchar(2048) DEFAULT '';
                    DECLARE required_tag_count int DEFAULT 0;
                    DECLARE candidate_limit int DEFAULT 500;
                    DECLARE occurrence_index int DEFAULT 1;
                    DECLARE term varchar(128);
                    DECLARE max_distance int;

                    SET page_offset = GREATEST(IFNULL(page_offset, 0), 0);
                    SET page_limit = LEAST(GREATEST(IFNULL(page_limit, 10), 1), 100);
                    SET candidate_limit = LEAST(500, GREATEST((page_offset + page_limit) * 10, 100));
                    SET tag_names = NULLIF(TRIM(BOTH ',' FROM IFNULL(tag_names, '')), '');

                    DROP TEMPORARY TABLE IF EXISTS PostSearchTerms;
                    CREATE TEMPORARY TABLE PostSearchTerms
                    (
                        Term varchar(128) NOT NULL PRIMARY KEY,
                        MaxDistance int NOT NULL
                    ) ENGINE=MEMORY;

                    term_loop: LOOP
                        SET term = REGEXP_SUBSTR(LOWER(search_text), '[[:alnum:]]+', 1, occurrence_index);

                        IF term IS NULL THEN
                            LEAVE term_loop;
                        END IF;

                        SET max_distance = CASE
                            WHEN CHAR_LENGTH(term) < 4 THEN 0
                            WHEN CHAR_LENGTH(term) <= 7 THEN 1
                            ELSE 2
                        END;

                        INSERT IGNORE INTO PostSearchTerms (Term, MaxDistance)
                        VALUES (LEFT(term, 128), max_distance);

                        SET full_text_query = CONCAT_WS(' ', NULLIF(full_text_query, ''), CONCAT(term, '*'));
                        SET occurrence_index = occurrence_index + 1;
                    END LOOP;

                    SET full_text_query = NULLIF(full_text_query, '');

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

                    DROP TEMPORARY TABLE IF EXISTS PostSearchCandidates;
                    CREATE TEMPORARY TABLE PostSearchCandidates
                    (
                        PostId char(36) NOT NULL PRIMARY KEY,
                        SortDate datetime(6) NOT NULL,
                        SearchText longtext NOT NULL
                    );

                    INSERT IGNORE INTO PostSearchCandidates (PostId, SortDate, SearchText)
                    SELECT
                        p.Id,
                        p.PublishedAt,
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
                        )
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
                    ORDER BY p.PublishedAt DESC, p.Id DESC
                    LIMIT candidate_limit;

                    CALL ApplyLoosePostSearchMatches();

                    SELECT PostId
                    FROM PostSearchResults
                    ORDER BY SortDate DESC, PostId DESC
                    LIMIT page_offset, page_limit;

                    DROP TEMPORARY TABLE IF EXISTS PostSearchCandidates;
                    DROP TEMPORARY TABLE IF EXISTS PostSearchTerms;
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
                SQL SECURITY INVOKER
                BEGIN
                    DECLARE full_text_query varchar(2048) DEFAULT '';
                    DECLARE candidate_limit int DEFAULT 500;
                    DECLARE occurrence_index int DEFAULT 1;
                    DECLARE term varchar(128);
                    DECLARE max_distance int;

                    SET page_offset = GREATEST(IFNULL(page_offset, 0), 0);
                    SET page_limit = LEAST(GREATEST(IFNULL(page_limit, 10), 1), 100);
                    SET candidate_limit = LEAST(500, GREATEST((page_offset + page_limit) * 10, 100));
                    SET post_status = NULLIF(TRIM(IFNULL(post_status, '')), '');

                    DROP TEMPORARY TABLE IF EXISTS PostSearchTerms;
                    CREATE TEMPORARY TABLE PostSearchTerms
                    (
                        Term varchar(128) NOT NULL PRIMARY KEY,
                        MaxDistance int NOT NULL
                    ) ENGINE=MEMORY;

                    term_loop: LOOP
                        SET term = REGEXP_SUBSTR(LOWER(search_text), '[[:alnum:]]+', 1, occurrence_index);

                        IF term IS NULL THEN
                            LEAVE term_loop;
                        END IF;

                        SET max_distance = CASE
                            WHEN CHAR_LENGTH(term) < 4 THEN 0
                            WHEN CHAR_LENGTH(term) <= 7 THEN 1
                            ELSE 2
                        END;

                        INSERT IGNORE INTO PostSearchTerms (Term, MaxDistance)
                        VALUES (LEFT(term, 128), max_distance);

                        SET full_text_query = CONCAT_WS(' ', NULLIF(full_text_query, ''), CONCAT(term, '*'));
                        SET occurrence_index = occurrence_index + 1;
                    END LOOP;

                    SET full_text_query = NULLIF(full_text_query, '');

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

                    DROP TEMPORARY TABLE IF EXISTS PostSearchCandidates;
                    CREATE TEMPORARY TABLE PostSearchCandidates
                    (
                        PostId char(36) NOT NULL PRIMARY KEY,
                        SortDate datetime(6) NOT NULL,
                        SearchText longtext NOT NULL
                    );

                    INSERT IGNORE INTO PostSearchCandidates (PostId, SortDate, SearchText)
                    SELECT
                        p.Id,
                        p.CreatedAt,
                        CONCAT_WS(' ', p.Title, p.Subtitle, p.Slug, d.PlainText)
                    FROM Posts p
                    LEFT JOIN PostMarkdownDrafts d ON d.PostId = p.Id AND d.IsDeleted = false
                    WHERE full_text_query IS NOT NULL
                        AND p.IsDeleted = false
                        AND (post_status IS NULL OR p.Status = post_status)
                    ORDER BY p.CreatedAt DESC, p.Id DESC
                    LIMIT candidate_limit;

                    CALL ApplyLoosePostSearchMatches();

                    SELECT PostId
                    FROM PostSearchResults
                    ORDER BY SortDate DESC, PostId DESC
                    LIMIT page_offset, page_limit;

                    DROP TEMPORARY TABLE IF EXISTS PostSearchCandidates;
                    DROP TEMPORARY TABLE IF EXISTS PostSearchTerms;
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
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS ApplyLoosePostSearchMatches;");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS PostSearchIsLooseMatch;");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS PostSearchLevenshteinDistance;");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS NormalizePostSearchBooleanQuery;");
        }
    }
}
