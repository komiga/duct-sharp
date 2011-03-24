#!/bin/bash

# (message)
pinfo() {
	echo "[compile] $1"
}

# (message)
perror() {
	echo "[compile] error: $1"
}

# (cmd)
erun() {
	echo "[compile] exec: $@"
	$@
}

COMPILER=gmcs
PATHS="-lib:../lib/debug"
FLAGS="-r:ductsharp.dll -debug"
LIBPATH="../lib/debug/ductsharp.dll"

compile() {
	filepath=$1
	if [ ! -f "$filepath" ]; then
		perror "File does not exist: \"$file\""
		return 3
	fi
	root=$(dirname "$filepath")
	file=${filepath%.*}
	cp "$LIBPATH" "$root/"
	cp "$LIBPATH.mdb" "$root/"
	pinfo "Compiling $filepath"
	erun $COMPILER $PATHS $FLAGS "$filepath" "-out:$file.exe"
	ec=$?
	if [ ! $ec == 0 ]; then
		perror "Compile failed with exit code: $ec"
		return 4
	fi
	return 0
}

if [ $# == 0 ]; then
	perror "No files given"
	exit 1
fi

if [ ! -f "$LIBPATH" ]; then
	perror "duct# must be built first"
	exit 2
fi

for file in "$@"
do
	compile "$file"
done

