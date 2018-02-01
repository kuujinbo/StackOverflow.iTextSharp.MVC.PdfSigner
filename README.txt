 Web context client-side digital signing. 
 
 Implementation specific, and *REQUIRES** the following:
      -- Internet Explorer web browser (tested IE >= version 10)
      -- CAPICOM is installed on client machines. CAPICOM support was 
         officially abandoned with the release of Windows 7, but
         is still available in many standard corporate desktop builds.
 
 Background reference here:
 http://stackoverflow.com/questions/28949243
===========================================================================
To enable client certificates in IIS Express for Visual Studio 2012/2013 open:

%USERPROFILE%\Documents\IISExpress\config\applicationhost.config

Visual Studio 2015 made breaking changes to the project config file, now in the project subdirectory:
./.vs/config/applicationhost.config

Comment-out default settings and make following changes:

1. add binding protocol for site ID:
        <binding protocol="https" bindingInformation="*:44301:localhost" />

2. edit iisClientCertificateMappingAuthentication node:
            <iisClientCertificateMappingAuthentication enabled="true">
            <!--<iisClientCertificateMappingAuthentication enabled="false">-->

3. edit sslFlags node:
            <access sslFlags="SslNegotiateCert" />
            <!--<access sslFlags="None" />-->

More info:

* http://www.jasonrshaver.com/post/wp7%20client%20certificates%20part%202%20%28client%20certs%20on%20the%20browser%29
* http://www.hanselman.com/blog/WorkingWithSSLAtDevelopmentTimeIsEasierWithIISExpress.aspx
* http://forums.iis.net/t/1210695.aspx?Visual+Studio+2012+Update+4+broke+IIS+Express+ability+to+require+client+certs+for+SSL
===========================================================================
Make a localhost SSL certificate to get rid of 'untrusted site' warning.
Path to 'makecert.exe' may be different on your system.

1. Open command prompt as __administrator__ and run following commands:

    a. "C:\Program Files (x86)\Windows Kits\8.1\bin\x64\makecert" -n "CN=localhost" -r -sv localhostCA.pvk localhostCA.cer

    b. "C:\Program Files (x86)\Windows Kits\8.1\bin\x64\makecert" -pe -ss My -sr CurrentUser -a sha1 -sky exchange -n CN=localhost -sk SignedByLocalHostCA -ic localhostCA.cer -iv localhostCA.pvk

2. Run command:start LocalhostCA.cer

    a. Click 'Install Certificate'
    
    b. Select 'Place all certificates in the following store', then 'Browse…'

    c. Select 'Show physical stores' checkbox.
    
    d. Select 'Trusted Root Certification Authorities => 'Local Computer'.
===========================================================================
if applicationhost.config gets hosed, with administrator account:

-- HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\SecurityProviders\SCHANNEL 
-- New => DWORD Value
-- Registry key name => 'SendTrustedIssuerList'
-- Value => 0

e.g. side-by-side vs2012/2013 install; uninstalled 2012, which broke 
client certificate prompt.