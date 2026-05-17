#requires -Version 7.0

param(
    [string] $BaseUrl = "http://localhost:5000",
    [int] $Count = 30,
    [string] $RunId = (Get-Date -Format "yyyyMMdd-HHmmss"),
    [string] $Login = "superadmin",
    [string] $Password = "superadmin"
)

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Drawing.Common

$tempRoot = Join-Path $env:TEMP "zerox2c-blog-seed-$RunId"
New-Item -ItemType Directory -Path $tempRoot -Force | Out-Null

function Write-SeedLog {
    param([string] $Message)
    Write-Host "[$(Get-Date -Format 'HH:mm:ss')] $Message"
}

function Invoke-JsonApi {
    param(
        [Parameter(Mandatory)] [string] $Path,
        [Parameter(Mandatory)] [string] $Method,
        [object] $Body,
        [hashtable] $Headers
    )

    $parameters = @{
        Uri = "$BaseUrl$Path"
        Method = $Method
        ContentType = "application/json"
    }

    if ($Headers) {
        $parameters.Headers = $Headers
    }

    if ($null -ne $Body) {
        $parameters.Body = $Body | ConvertTo-Json -Depth 16
    }

    Invoke-RestMethod @parameters
}

function New-SeedImage {
    param(
        [Parameter(Mandatory)] [string] $Path,
        [Parameter(Mandatory)] [int] $Width,
        [Parameter(Mandatory)] [int] $Height,
        [Parameter(Mandatory)] [string] $Label,
        [Parameter(Mandatory)] [string] $Kind,
        [Parameter(Mandatory)] [int] $Seed
    )

    $palette = @(
        @([System.Drawing.Color]::FromArgb(28, 87, 126), [System.Drawing.Color]::FromArgb(242, 196, 90)),
        @([System.Drawing.Color]::FromArgb(34, 116, 96), [System.Drawing.Color]::FromArgb(235, 235, 220)),
        @([System.Drawing.Color]::FromArgb(123, 44, 61), [System.Drawing.Color]::FromArgb(246, 209, 103)),
        @([System.Drawing.Color]::FromArgb(60, 64, 91), [System.Drawing.Color]::FromArgb(129, 178, 154)),
        @([System.Drawing.Color]::FromArgb(32, 32, 32), [System.Drawing.Color]::FromArgb(231, 111, 81)),
        @([System.Drawing.Color]::FromArgb(25, 92, 99), [System.Drawing.Color]::FromArgb(255, 243, 176)),
        @([System.Drawing.Color]::FromArgb(79, 70, 229), [System.Drawing.Color]::FromArgb(167, 243, 208)),
        @([System.Drawing.Color]::FromArgb(88, 28, 135), [System.Drawing.Color]::FromArgb(253, 224, 71))
    )

    $colors = $palette[$Seed % $palette.Count]
    $bitmap = [System.Drawing.Bitmap]::new($Width, $Height)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $graphics.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit

    try {
        $rect = [System.Drawing.Rectangle]::new(0, 0, $Width, $Height)
        $background = [System.Drawing.Drawing2D.LinearGradientBrush]::new(
            $rect,
            $colors[0],
            $colors[1],
            [System.Drawing.Drawing2D.LinearGradientMode]::ForwardDiagonal
        )
        $graphics.FillRectangle($background, $rect)
        $background.Dispose()

        $random = [System.Random]::new(($Seed * 7919) + $Width + $Height)
        for ($i = 0; $i -lt 18; $i++) {
            $brush = [System.Drawing.SolidBrush]::new(
                [System.Drawing.Color]::FromArgb(32 + $random.Next(64), 255, 255, 255)
            )
            $x = $random.Next(-80, [Math]::Max($Width - 40, 1))
            $y = $random.Next(-80, [Math]::Max($Height - 40, 1))
            $size = $random.Next([Math]::Max([int]($Width / 12), 30), [Math]::Max([int]($Width / 4), 60))
            $shapeHeight = [Math]::Max([int]($size * 0.58), 20)
            if ($i % 2 -eq 0) {
                $graphics.FillEllipse($brush, $x, $y, $size, $shapeHeight)
            } else {
                $graphics.FillRectangle($brush, $x, $y, $size, $shapeHeight)
            }
            $brush.Dispose()
        }

        $padding = [Math]::Max([int]($Width * 0.045), 28)
        $panelHeight = [Math]::Max([int]($Height * 0.28), 90)
        $panelBrush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(140, 0, 0, 0))
        $graphics.FillRectangle($panelBrush, $padding, $Height - $panelHeight - $padding, $Width - ($padding * 2), $panelHeight)
        $panelBrush.Dispose()

        $fontSize = [Math]::Max([int]($Height * 0.08), 28)
        $titleFont = [System.Drawing.Font]::new("Segoe UI", $fontSize, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
        $metaFont = [System.Drawing.Font]::new("Segoe UI", [Math]::Max([int]($fontSize * 0.42), 14), [System.Drawing.FontStyle]::Regular, [System.Drawing.GraphicsUnit]::Pixel)
        $white = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::White)
        $muted = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(220, 255, 255, 255))
        $graphics.DrawString($Label, $titleFont, $white, [float]($padding + 24), [float]($Height - $panelHeight - $padding + 20))
        $graphics.DrawString("$Kind image - generated fixture asset", $metaFont, $muted, [float]($padding + 28), [float]($Height - $panelHeight - $padding + 32 + $fontSize))

        $white.Dispose()
        $muted.Dispose()
        $titleFont.Dispose()
        $metaFont.Dispose()
        $bitmap.Save($Path, [System.Drawing.Imaging.ImageFormat]::Png)
    }
    finally {
        $graphics.Dispose()
        $bitmap.Dispose()
    }
}

function Upload-AdminImage {
    param(
        [Parameter(Mandatory)] [string] $Purpose,
        [Parameter(Mandatory)] [string] $Path,
        [Parameter(Mandatory)] [hashtable] $Headers
    )

    $response = & curl.exe -sS -f `
        -X POST `
        -H "Authorization: $($Headers.Authorization)" `
        -F "file=@$Path;type=image/png" `
        -F "purpose=$Purpose" `
        "$BaseUrl/api/admin/images"

    if ($LASTEXITCODE -ne 0) {
        throw "Image upload failed for $Path."
    }

    $response | ConvertFrom-Json
}

function Upload-EmbeddedImage {
    param(
        [Parameter(Mandatory)] [string] $PostId,
        [Parameter(Mandatory)] [string] $Path,
        [Parameter(Mandatory)] [hashtable] $Headers
    )

    $response = & curl.exe -sS -f `
        -X POST `
        -H "Authorization: $($Headers.Authorization)" `
        -F "file=@$Path;type=image/png" `
        "$BaseUrl/api/admin/posts/$PostId/markdown/images"

    if ($LASTEXITCODE -ne 0) {
        throw "Embedded image upload failed for $Path."
    }

    $response | ConvertFrom-Json
}

function New-Markdown {
    param(
        [Parameter(Mandatory)] [string] $Title,
        [Parameter(Mandatory)] [string] $Topic,
        [Parameter(Mandatory)] [int] $Index,
        [AllowEmptyCollection()] [object[]] $EmbeddedImages = @()
    )

    $imageBlocks = foreach ($image in $EmbeddedImages) {
        "![Generated embedded image for $Topic]($($image.localPath))`n`nThis inline media block is intentionally surrounded by text so the frontend can exercise image spacing, loading states, captions nearby, and scroll behavior."
    }

    $codeLang = @("typescript", "csharp", "powershell", "sql", "bash")[$Index % 5]
    $code = switch ($codeLang) {
        "typescript" { "const samples = posts.slice(-24);`nconsole.log(samples.map(post => post.slug));" }
        "csharp" { "var published = posts.Where(post => post.Status == PostStatus.Published);`nforeach (var post in published) Console.WriteLine(post.Slug);" }
        "powershell" { "Get-ChildItem -Recurse -Filter *.md | Select-String -Pattern 'latency|cache|search'" }
        "sql" { "select slug, published_at from posts where status = 'Published' order by published_at desc;" }
        default { "curl -H 'accept: application/json' http://localhost:5000/api/posts" }
    }

@"
# $Title

This seeded article explores **$Topic** from the perspective of a working technical blog. It mixes short paragraphs, dense details, lists, tables, code, and media so the reader frontend has realistic layout pressure.

## Field Notes

Production writing rarely arrives as one clean block of prose. It contains references, asides, numbered steps, images, and sections that vary in length.

- The opening needs to scan well on mobile.
- Tags should feel like real browsing anchors.
- Media should exercise cover, banner, and inline image handling.

> A practical interface should remain calm when the content becomes uneven.

## Implementation Sketch

````$codeLang
$code
````

## Tradeoff Table

| Area | Useful Signal | Failure Mode |
| --- | --- | --- |
| Navigation | Tags and titles make browsing predictable | Overlong labels push layout apart |
| Reading | Paragraph rhythm helps scanning | Dense prose becomes visually flat |
| Media | Images provide landmarks | Missing dimensions can cause jumps |
| Search | Titles, subtitles, and tags explain why a result matched | Generic summaries feel duplicated |

$($imageBlocks -join "`n`n")

## Closing Notes

For this run, post number $Index deliberately uses varied content structure. It should help reveal spacing issues, image loading states, route transitions, and article readability problems without waiting for hand-written production posts.
"@
}

try {
    Write-SeedLog "Logging in to $BaseUrl as $Login."
    $auth = Invoke-JsonApi -Path "/api/auth/login" -Method Post -Body @{
        login = $Login
        password = $Password
    }
    $headers = @{ Authorization = "Bearer $($auth.accessToken)" }

    $tagDefinitions = @(
        @{ name = "dotnet"; description = ".NET backend notes and API implementation details." },
        @{ name = "vue"; description = "Vue frontend patterns and reader experience work." },
        @{ name = "typescript"; description = "TypeScript implementation notes." },
        @{ name = "database"; description = "Persistence, migrations, indexes, and query behavior." },
        @{ name = "security"; description = "Authentication, authorization, and operational safety." },
        @{ name = "frontend"; description = "Public website UX and rendering concerns." },
        @{ name = "backend"; description = "Server-side architecture and API behavior." },
        @{ name = "devlog"; description = "Project updates and implementation diary entries." },
        @{ name = "ops"; description = "Deployment, runtime, and service operations." },
        @{ name = "design"; description = "Product and interface design notes." }
    )

    Write-SeedLog "Ensuring tags exist."
    $tagMap = @{}
    $existingTags = Invoke-JsonApi -Path "/api/admin/tags?limit=100" -Method Get -Headers $headers
    foreach ($tag in $existingTags) {
        $tagMap[$tag.name] = $tag
    }

    foreach ($definition in $tagDefinitions) {
        if (-not $tagMap.ContainsKey($definition.name)) {
            $created = Invoke-JsonApi -Path "/api/admin/tags" -Method Post -Headers $headers -Body $definition
            $tagMap[$created.name] = $created
        }
    }

    $topics = @(
        "API pagination under real browsing pressure",
        "Markdown rendering with uneven article shapes",
        "Image-heavy technical writing layouts",
        "Admin workflows for publishing drafts",
        "Tag browsing and discovery loops",
        "Search result context and tags",
        "Database migrations during local development",
        "JWT authentication boundaries",
        "Vue route transitions for articles",
        "Reader typography on narrow screens",
        "Caching choices for public content",
        "Operational checklists for weekend deploys",
        "Code block readability in long posts",
        "Responsive cover art behavior",
        "Post cards with missing and rich metadata",
        "Backend validation messages in admin tools",
        "Publishing snapshots versus drafts",
        "Inline diagrams inside Markdown",
        "Navigation density on a personal site",
        "Content fixtures for frontend iteration",
        "Audited entity writes and authorship",
        "Local Docker MySQL development",
        "Slug design and collision handling",
        "Accessibility checks for article pages",
        "Rendering tables without breaking mobile",
        "Steam login notes for side projects",
        "Error states that still teach users",
        "Public API contracts for readers",
        "Admin media uploads and previews",
        "Release notes for a small blog platform"
    )

    $createdPosts = @()
    for ($index = 1; $index -le $Count; $index++) {
        $topic = $topics[($index - 1) % $topics.Count]
        $title = "Seed $($index.ToString('00')): $topic"
        $slugTopic = $topic.ToLowerInvariant() -replace "[^a-z0-9]+", "-" -replace "(^-|-$)", ""
        $slug = "seed-$RunId-$($index.ToString('00'))-$slugTopic"
        if ($slug.Length -gt 150) {
            $slug = $slug.Substring(0, 150).TrimEnd("-")
        }

        $coverPath = Join-Path $tempRoot "cover-$($index.ToString('00')).png"
        $bannerPath = Join-Path $tempRoot "banner-$($index.ToString('00')).png"
        New-SeedImage -Path $coverPath -Width 1200 -Height 675 -Label "Seed $($index.ToString('00'))" -Kind "Cover" -Seed ($index * 11)
        New-SeedImage -Path $bannerPath -Width 1600 -Height 520 -Label $topic -Kind "Banner" -Seed ($index * 17)

        $cover = Upload-AdminImage -Purpose "Cover" -Path $coverPath -Headers $headers
        $banner = Upload-AdminImage -Purpose "Banner" -Path $bannerPath -Headers $headers

        $tagNames = @(
            $tagDefinitions[($index - 1) % $tagDefinitions.Count].name,
            $tagDefinitions[($index + 2) % $tagDefinitions.Count].name,
            $tagDefinitions[($index + 5) % $tagDefinitions.Count].name
        ) | Select-Object -Unique

        $tagIds = @($tagNames | ForEach-Object { $tagMap[$_].id })
        $subtitle = @(
            "A generated post with enough texture to exercise the public reader UI.",
            "Fixture content for layout, routing, media, and tag browsing checks.",
            "Synthetic but structured writing for frontend feature work.",
            "Local seed data covering Markdown, images, tags, and publishing."
        )[($index - 1) % 4]

        $post = Invoke-JsonApi -Path "/api/admin/posts" -Method Post -Headers $headers -Body @{
            slug = $slug
            title = $title
            subtitle = $subtitle
            bodyMarkdown = "# $title`n`nInitial draft body used before attaching post-scoped embedded images."
            coverImageId = $cover.id
            bannerImageId = $banner.id
            tagIds = $tagIds
        }

        $embeddedImages = @()
        $embeddedCount = ($index - 1) % 4
        for ($embeddedIndex = 1; $embeddedIndex -le $embeddedCount; $embeddedIndex++) {
            $embeddedPath = Join-Path $tempRoot "embedded-$($index.ToString('00'))-$embeddedIndex.png"
            New-SeedImage -Path $embeddedPath -Width 960 -Height 540 -Label "Inline $($index.ToString('00')).$embeddedIndex" -Kind "Embedded" -Seed (($index * 101) + ($embeddedIndex * 13))
            $embeddedImages += Upload-EmbeddedImage -PostId $post.id -Path $embeddedPath -Headers $headers
        }

        $body = New-Markdown -Title $title -Topic $topic -Index $index -EmbeddedImages $embeddedImages
        Invoke-JsonApi -Path "/api/admin/posts/$($post.id)" -Method Put -Headers $headers -Body @{
            slug = $slug
            title = $title
            subtitle = $subtitle
            bodyMarkdown = $body
            coverImageId = $cover.id
            bannerImageId = $banner.id
            tagIds = $tagIds
        } | Out-Null

        $published = Invoke-JsonApi -Path "/api/admin/posts/$($post.id)/publish" -Method Post -Headers $headers
        $createdPosts += $published
        Write-SeedLog "Published $($index.ToString('00'))/${Count}: $slug ($embeddedCount embedded images)."
    }

    $publicPosts = Invoke-JsonApi -Path "/api/posts?limit=100" -Method Get
    $visible = @($publicPosts | Where-Object { $_.slug -like "seed-$RunId-*" })

    Write-Host ""
    Write-Host "Seed run: $RunId"
    Write-Host "Created and published posts: $($createdPosts.Count)"
    Write-Host "Publicly visible from this run: $($visible.Count)"
}
finally {
    if (Test-Path $tempRoot) {
        Remove-Item -LiteralPath $tempRoot -Recurse -Force
    }
}
