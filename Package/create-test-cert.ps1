$certPassword = ConvertTo-SecureString -String "GhostDrawTest123!" -Force -AsPlainText

$cert = New-SelfSignedCertificate `
    -Type Custom `
    -Subject "CN=RuntimeRascal Test Certificate" `
    -KeyUsage DigitalSignature `
    -FriendlyName "GhostDraw Test Certificate" `
    -CertStoreLocation "Cert:\CurrentUser\My" `
    -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.3", "2.5.29.19={text}")

$thumbprint = $cert.Thumbprint

# Export certificate
Export-PfxCertificate `
    -Cert "Cert:\CurrentUser\My\$thumbprint" `
    -FilePath "GhostDraw_TestCert.pfx" `
    -Password $certPassword

Export-Certificate `
    -Cert "Cert:\CurrentUser\My\$thumbprint" `
    -FilePath "GhostDraw_TestCert.cer"

Write-Host "Test certificate created:" -ForegroundColor Green
Write-Host "  PFX: GhostDraw_TestCert.pfx (password: GhostDrawTest123!)" -ForegroundColor Gray
Write-Host "  CER: GhostDraw_TestCert.cer" -ForegroundColor Gray
Write-Host ""
Write-Host "Thumbprint: $thumbprint" -ForegroundColor Yellow
Write-Host ""
Write-Host "To install certificate for testing:" -ForegroundColor Cyan
Write-Host "  1. Double-click GhostDraw_TestCert.cer" -ForegroundColor White
Write-Host "  2. Click 'Install Certificate...'" -ForegroundColor White
Write-Host "  3. Select 'Local Machine' -> Next" -ForegroundColor White
Write-Host "  4. Select 'Place all certificates in the following store'" -ForegroundColor White
Write-Host "  5. Browse -> Select 'Trusted Root Certification Authorities'" -ForegroundColor White
Write-Host "  6. Finish" -ForegroundColor White
