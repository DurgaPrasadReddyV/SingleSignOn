<# Check development certificates #>

if (! (  Test-Path ".\etc\dev-cert\localhost.pfx" -PathType Leaf ) ){
    Write-Information "Creating dev certificates..."
    Set-Location ".\etc\dev-cert"
    dotnet dev-certs https -v -ep localhost.pfx -p 8b6039b6-c67a-448b-977b-0ce6d3fcfd49 -t  
    Set-Location ..
    Set-Location ..
 }
 
 <# Run all services #>
 
 tye run --watch