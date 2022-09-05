# IpnsSync
Watch an IPNS url and automatically download files to local disk.

A quick script thrown together in about an hour. Requires Kubo to be running.

# How to use

### Clone the repo

```
git clone https://github.com/Arlodotexe/ipnssync
cd ipnssync
```

### Run the script
Requires .NET 6 or greater.

Example usage. 
Replace the value of `--ipns` and `--output-path` with the desired values.

```powershell
dotnet run --ipns=ipfs.tech --output-path=/home/pi/ipfs.tech/

dotnet run --output-path="C:\Users\MyUser\Downloads\ipfs.tech\" --ipns=k51qzi5uqu5dip7dqovvkldk0lz03wjkc2cndoskxpyh742gvcd5fw4mudsorj

# Additional options
dotnet run --api=http://127.0.0.1:5002 --output-path=E:\ --ipns=latest.strixmusic.com --interval-seconds=1800
```

