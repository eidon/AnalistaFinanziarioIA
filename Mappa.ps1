function Show-Tree {
    param($Path, $Indent = "")
    
    $Items = Get-ChildItem -Path $Path | Where-Object { 
        # Esclude cartelle standard, Migrations e file GlobalSuppressions
        $_.Name -notmatch '^(bin|obj|debug|release|.git|.vs|node_modules|Migrations|GlobalSuppressions\.cs)$' -and 
        # ESCLUDE specificamente wwwroot\js
        $_.FullName -notmatch '\\wwwroot\\js$' -and
        ($_.PSIsContainer -or $_.Extension -match "\.(cs|js|ts|html|json)$") 
    }
    
    foreach ($item in $Items) {
        $IsLast = ($item.Name -eq $Items[-1].Name)
        $Prefix = "├── "; $AddIndent = "│   "
        if ($IsLast) { $Prefix = "└── "; $AddIndent = "    " }
        
        $Icon = "📄 "
        if ($item.PSIsContainer) { $Icon = "📁 " }
        elseif ($item.Extension -eq ".cs") { $Icon = "🟦 " }
        elseif ($item.Extension -eq ".js" -or $item.Extension -eq ".ts") { $Icon = "🟨 " }
        
        Add-Content -Path "struttura_progetto.txt" -Value ($Indent + $Prefix + $Icon + $item.Name) -Encoding UTF8
        $SubIndent = $Indent + $AddIndent

        if ($item.PSIsContainer) {
            Show-Tree $item.FullName $SubIndent
        } 
        elseif ($item.Extension -eq ".cs") {
            $Lines = Get-Content $item.FullName
            foreach ($line in $Lines) {
                $t = $line.Trim()
                
                # 1. DECORATOR
                if ($t -match "^\[.*\]$") {
                    Add-Content -Path "struttura_progetto.txt" -Value ($SubIndent + "    🏷️  " + $t) -Encoding UTF8
                }
                # 2. Classi
                elseif ($t -match "^\s*(public|private|internal|static|partial)\s+(class|interface|struct|enum)\s+\w+") {
                    $clean = $t.Split('{')[0].Trim()
                    Add-Content -Path "struttura_progetto.txt" -Value ($SubIndent + "    🔲 " + $clean) -Encoding UTF8
                }
                # 3. PROPRIETÀ
                elseif ($t -match "^\s*(public|private|internal|protected|static).+\s\w+\s*(\{.*get|=>|=)") {
                    $clean = $t.Split('{')[0].Split('=')[0].Split('=>')[0].Trim()
                    Add-Content -Path "struttura_progetto.txt" -Value ($SubIndent + "    🔧 " + $clean) -Encoding UTF8
                }
                # 4. Metodi
                elseif ($t -match "^\s*(public|private|internal|protected|static|override|virtual)\s+[\w<>\?\[\]]+\s+\w+\s*\(") {
                    if ($t -notmatch "if|for|foreach|while|switch") {
                        $clean = $t.Split('{')[0].Trim()
                        Add-Content -Path "struttura_progetto.txt" -Value ($SubIndent + "    💜 " + $clean) -Encoding UTF8
                    }
                }
            }
        }
        elseif ($item.Extension -eq ".ts") {
            $Lines = Get-Content $item.FullName
            foreach ($line in $Lines) {
                $t = $line.Trim()
                if ($t -match "^\s*(export\s+)?(class|interface|enum|type)\s+\w+") {
                    $clean = $t.Split('{')[0].Trim()
                    Add-Content -Path "struttura_progetto.txt" -Value ($SubIndent + "    🔲 " + $clean) -Encoding UTF8
                }
                elseif ($t -match "^\s*(public|private|protected|static|async|function)?\s*\w+\s*\(.*\)\s*(:|\{)?") {
                    if ($t -notmatch "import|if|for|while|switch|return|=>") {
                        $clean = $t.Split('{|(')[0].Trim()
                        Add-Content -Path "struttura_progetto.txt" -Value ($SubIndent + "    💜 " + $clean + "()") -Encoding UTF8
                    }
                }
                elseif ($t -match "^\s*(public|private|protected|readonly)?\s*\w+\s*\??\s*:\s*[^;{]+") {
                    $clean = $t.Trim(';').Trim()
                    Add-Content -Path "struttura_progetto.txt" -Value ($SubIndent + "    🔧 " + $clean) -Encoding UTF8
                }
            }
        }
    }
}

# --- ESECUZIONE ---
$fileRisultato = "struttura_progetto.txt"
if (Test-Path $fileRisultato) { Remove-Item $fileRisultato }

$currentPath = Get-Location
Show-Tree $currentPath

Write-Host "Mappa generata con successo (escluse Migrations, GlobalSuppressions e wwwroot/js)!" -ForegroundColor Green
ii $fileRisultato
