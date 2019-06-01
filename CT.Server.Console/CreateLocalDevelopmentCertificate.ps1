Write-Output "`r`nCompositactic Certificate Tool"

$certificateFriendlyName = Read-Host "Enter the FriendlyName of the certificate"

$certificate = Get-ChildItem -Path cert:\* -Recurse | Where-Object { $_.FriendlyName -eq $certificateFriendlyName }

if (!$certificate) { 

	$msg = 'Certificate not found. Create? [Y/N]'
	do {
		$response = Read-Host -Prompt $msg
		if ($response -eq 'y') {
			$certificate = New-SelfSignedCertificate `
				-Subject localhost `
				-DnsName localhost `
				-KeyAlgorithm RSA `
				-KeyLength 2048 `
				-NotBefore (Get-Date) `
				-NotAfter (Get-Date).AddYears(10) `
				-CertStoreLocation "cert:CurrentUser\My" `
				-FriendlyName $certificateFriendlyName `
				-HashAlgorithm SHA256 `
				-KeyUsage DigitalSignature, KeyEncipherment, DataEncipherment `
				-TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.1") 

			$certificatePath = 'Cert:\CurrentUser\My\' + ($certificate.ThumbPrint)    

			$pfxPassword = ConvertTo-SecureString ([Guid]::NewGuid().ToString()) -Force -AsPlainText
			$pfxFilePath =  [system.io.path]::GetTempFileName()
			$cerFilePath = [system.io.path]::GetTempFileName()

			Export-PfxCertificate -Cert $certificatePath -FilePath $pfxFilePath -Password $pfxPassword
			Export-Certificate -Cert $certificatePath -FilePath $cerFilePath

			Remove-Item $certificatePath
			Import-PfxCertificate -FilePath $pfxFilePath Cert:\LocalMachine\My -Password $pfxPassword -Exportable
			Import-Certificate -FilePath $cerFilePath -CertStoreLocation Cert:\CurrentUser\Root

			Remove-Item $pfxFilePath
			Remove-Item $cerFilePath
			Break
		}
	} until ($response -eq 'n')
}

if (!$certificate) { 
	Exit
}

[int]$port = Read-Host -Prompt 'Enter port number for https'

$command = "http delete sslcert ipport=0.0.0.0:$port"
Write-Output $command
$command | netsh
    
$command = "http add sslcert ipport=0.0.0.0:$port certhash="+$($certificate.Thumbprint)+" appid={214124cd-d05b-4309-9af9-9caa44b2b74a}"
Write-Output $command
$command | netsh


