Output .vhs/site-index-dry-run.gif

Set Shell "zsh"
Set FontSize 14
Set Width 1400
Set Height 900
Set Padding 20
Set Theme "Catppuccin Mocha"
Set TypingSpeed 40ms

Type "dotnet run --project src/tooling/crawl-indexer/ -- site index --fair --dry-run --max-pages 100 --rps 10 --no-ai"
Enter
Sleep 120s
