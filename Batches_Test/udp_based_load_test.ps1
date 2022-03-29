# see detail at "Advanced Feature" section in: https://malaybaku.github.io/VMagicMirror/en/docs/setting_files
# expected usage (on cmd.exe):
# powershell -NoProfile -ExecutionPolicy Unrestricted .\udp_based_load_test.ps1

$port = 45900
$json = @"
{
    "command": "load_setting_file",
    "args": 
    {
        "index": 1,
        "load_character": true,
        "load_non_character": false
    }
}
"@
$enc = [System.Text.Encoding]::UTF8
$bytes = $enc.GetBytes($json)

$client = New-Object System.Net.Sockets.UdpClient
$client.Connect("127.0.0.1", $port)
$client.Send($bytes, $bytes.Length)
$client.Close()
