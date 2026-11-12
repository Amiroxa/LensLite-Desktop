# Downloads Persian + English Tesseract models (Apache-2.0) into .\tessdata
# Choose "tessdata_fast" (small, quicker) or "tessdata_best" (larger, more accurate).
param([ValidateSet("tessdata_fast","tessdata_best")][string]$Set = "tessdata_best")
New-Item -ItemType Directory -Force -Path "$PSScriptRoot\tessdata" | Out-Null
foreach ($l in "fas","eng") {
  Invoke-WebRequest "https://github.com/tesseract-ocr/$Set/raw/main/$l.traineddata" -OutFile "$PSScriptRoot\tessdata\$l.traineddata"
}
