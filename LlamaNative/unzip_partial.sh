#!/bin/bash

find "$1" -name '*.zip' -execdir sh -c '
base=$(basename {} .zip)
if [ -f "${base}.z01" ]; then
  cat "${base}.z01" "${base}.zip" > "${base}_combined.zip"
  unzip -o "${base}_combined.zip"
  rm "${base}_combined.zip"
else
  unzip -o {}
fi' \;
