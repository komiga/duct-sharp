#!/bin/bash

# (message)
pinfo() {
	echo "[run] $1"
}

# (message)
perror() {
	echo "[run] error: $1"
}

# (cmd)
erun() {
	echo "[run] exec: $@"
	$@
}

if [ $# == 0 ]; then
	echo "No parameters given"
	exit 1
fi

path=$1
if [ ! -f "$path" ]; then
	perror "$path does not exist"
	exit 1

fi

#libpath=$(dirname `pwd`)/lib/debug/ductsharp.dll
execname=$(basename "$path")
cd $(dirname "$path")
#LD_LIBRARY_PATH=$LD_LIBRARY_PATH:$libpath mono "$execname" ${@#* }
erun mono --debug "$execname" $2 $3 $4 $5 $6 $7 $8
