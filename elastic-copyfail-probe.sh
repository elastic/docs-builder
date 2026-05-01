#!/usr/bin/env sh
set -u

echo "== elastic docs-builder Copy Fail eligibility probe =="
date -u 2>/dev/null || true

echo
echo "== identity =="
id || true
whoami 2>/dev/null || true
umask 2>/dev/null || true

echo
echo "== kernel =="
uname -a || true
cat /proc/version 2>/dev/null || true
cat /etc/os-release 2>/dev/null | sed -n '1,12p' || true

echo
echo "== process security context =="
grep -E '^(Uid|Gid|Groups|NoNewPrivs|Seccomp|Cap(Inh|Prm|Eff|Bnd|Amb)):' /proc/self/status 2>/dev/null || true
cat /proc/self/uid_map 2>/dev/null || true
cat /proc/self/gid_map 2>/dev/null || true
cat /proc/self/cgroup 2>/dev/null || true

echo
echo "== AF_ALG / crypto availability =="
grep -w 'authencesn' /proc/crypto 2>/dev/null || true
grep -w 'cbc(aes)' /proc/crypto 2>/dev/null || true

if command -v python3 >/dev/null 2>&1; then
  python3 - <<'PY' 2>&1 || true
import os
import socket

print("python:", os.sys.version.split()[0])
print("os.splice:", hasattr(os, "splice"))

try:
    s = socket.socket(38, socket.SOCK_SEQPACKET, 0)
    print("AF_ALG socket: ok")
    s.close()
except Exception as e:
    print("AF_ALG socket:", type(e).__name__, str(e))

try:
    s = socket.socket(38, socket.SOCK_SEQPACKET, 0)
    s.bind(("aead", "authencesn(hmac(sha256),cbc(aes))"))
    print("AF_ALG bind authencesn(hmac(sha256),cbc(aes)): ok")
    s.close()
except Exception as e:
    print("AF_ALG bind authencesn(hmac(sha256),cbc(aes)):", type(e).__name__, str(e))
PY
elif command -v python >/dev/null 2>&1; then
  python - <<'PY' 2>&1 || true
import os
import socket

print("python:", os.sys.version.split()[0])
print("os.splice:", hasattr(os, "splice"))

try:
    s = socket.socket(38, socket.SOCK_SEQPACKET, 0)
    print("AF_ALG socket: ok")
    s.close()
except Exception as e:
    print("AF_ALG socket:", type(e).__name__, str(e))

try:
    s = socket.socket(38, socket.SOCK_SEQPACKET, 0)
    s.bind(("aead", "authencesn(hmac(sha256),cbc(aes))"))
    print("AF_ALG bind authencesn(hmac(sha256),cbc(aes)): ok")
    s.close()
except Exception as e:
    print("AF_ALG bind authencesn(hmac(sha256),cbc(aes)):", type(e).__name__, str(e))
PY
else
  echo "python: not found"
fi

echo
echo "== read-only filesystem boundary checks =="
for p in /etc/passwd /etc/shadow /usr/bin/su /bin/su /usr/bin/passwd /bin/bash /proc/sys/vm/drop_caches; do
  if [ -e "$p" ]; then
    ls -l "$p" 2>/dev/null || true
    test -r "$p" && echo "readable: $p" || echo "not-readable: $p"
    test -w "$p" && echo "writable: $p" || echo "not-writable: $p"
  else
    echo "missing: $p"
  fi
done

echo
echo "== non-destructive verdict inputs =="
echo "Copy Fail needs: exact AF_ALG authencesn bind OK, os.splice or libc splice, readable target file, and a meaningful privilege boundary above the current runner user."
echo "This probe does not write page cache, change /etc/passwd, read secrets, or exfiltrate data."
