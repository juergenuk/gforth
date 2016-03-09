#!/bin/bash
#Copyright (C) 2011,2012,2013,2014,2015 Free Software Foundation, Inc.

#This file is part of Gforth.

#Gforth is free software; you can redistribute it and/or
#modify it under the terms of the GNU General Public License
#as published by the Free Software Foundation, either version 3
#of the License, or (at your option) any later version.

#This program is distributed in the hope that it will be useful,
#but WITHOUT ANY WARRANTY; without even the implied warranty of
#MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.#See the
#GNU General Public License for more details.

#You should have received a copy of the GNU General Public License
#along with this program. If not, see http://www.gnu.org/licenses/.

function extra_apps {
    for i in $EXTRADIRS
    do
	test -f $i/AndroidManifest/apps && cat $i/AndroidManifest/apps
    done
}

function extra_perms {
    for i in $EXTRADIRS
    do
	test -f $i/AndroidManifest/perms && cat $i/AndroidManifest/perms
    done
}

function extra_features {
    for i in $EXTRADIRS
    do
	test -f $i/AndroidManifest/app && cat $i/AndroidManifest/features
    done
}

. build.local
TOOLCHAIN=$(which $TARGET-gcc | sed -e s,/bin/.*-gcc,,g)
NDK=${NDK-~/proj/android-ndk-r10e}
SRC=$(cd ../../..; pwd)

mkdir -p build

cp $NDK/sources/android/cpufeatures/*.[ch] build/unix

while [ "${1%%[^\+]*}" == '+' ]
do
    arch+=" ${1#+}"
    shift
done

if [ ! -z "$arch" ]
then
    echo "Extra build in $arch"
fi

for i in $arch
do
    newdir=$SRC/arch/$i/android
    (cd $newdir && ./build.sh "$@")
done

if [ ! -f local.properties ]
then
    android update project -p . -s --target android-14
fi

# takes as extra argument a directory where to look for .so-s

ENGINES="gforth-fast gforth-itc"

GFORTH_VERSION=$($GFORTH_DITC --version 2>&1 | cut -f2 -d' ')
APP_VERSION=$[$(cat ~/.app-version)+1]
echo $APP_VERSION >~/.app-version

LIBCCNAMED=lib/$($GFORTH_DITC --version 2>&1 | cut -f1-2 -d ' ' | tr ' ' '/')/$machine/libcc-named/.libs

if [ ! -f $SRC/configure ]
then
    (cd $SRC; ./autogen.sh)
fi

rm -rf $LIBS
mkdir -p $LIBS

if [ ! -f $TOOLCHAIN/sysroot/usr/lib/libsoil2.a ]
then
    cp $TOOLCHAIN/sysroot/usr/lib/libsoil.so $LIBS
fi
cp .libs/libtypeset.so $LIBS

EXTRAS=""
EXTRADIRS=""
for i in $@
do
    EXTRAS+=" -with-extras=$i"
    EXTRADIRS+=" $i"
done

. ./AndroidManifest.xml.in >AndroidManifest.xml

if [ "$1" != "--no-gforthgz" ]
then
    (cd build
	if [ "$1" != "--no-config" ]
	then
	    $SRC/configure --host=$TARGET --with-cross=android --with-ditc=$GFORTH_DITC --prefix= --datarootdir=/sdcard --libdir=/sdcard/gforth/$machine --libexecdir=/lib --includedir=$PWD/include --enable-lib $EXTRAS || exit 1
	fi
	make || exit 1
	make prefix=$TOOLCHAIN/sysroot/usr install-include
	rm -rf debian/sdcard
	if [ "$1" != "--no-config" ]; then make extras || exit 1; fi
	make setup-debdist || exit 1) || exit 1
    if [ "$1" == "--no-config" ]
    then
	CONFIG=no; shift
    fi
    
    mkdir -p build/debian/sdcard/gforth/$machine/gforth/site-forth
    mkdir -p res/raw
    cp *.{fs,fi,png,jpg} build/debian/sdcard/gforth/$machine/gforth/site-forth
    (cd build/debian/sdcard
     mkdir -p gforth/home gforth/site-forth
     gforth archive.fs gforth/home/ gforth/site-forth/ $(find gforth/$GFORTH_VERSION -type f) $(find gforth/site-forth -type f)) | gzip -9 >res/raw/gforth
    (cd build/debian/sdcard
     rm gforth/$machine/lib*
     rm -rf gforth/$machine/gforth/$GFORTH_VERSION/$machine/libcc-named
     gforth archive.fs $machine/gforth/site-forth/ $(find gforth/$machine/gforth -type f)) | gzip -9 >$LIBS/libgforth-${machine}gz.so
else
    shift
fi

SHA256=$(sha256sum res/raw/gforth | cut -f1 -d' ')
SHA256ARCH=$(sha256sum $LIBS/libgforth-${machine}gz.so | cut -f1 -d' ')

for i in $ENGINES
do
    sed -e "s/sha256sum-sha256sum-sha256sum-sha256sum-sha256sum-sha256sum-sha2/$SHA256/" -e "s/sha256archsum-sha256archsum-sha256archsum-sha256archsum-sha256ar/$SHA256ARCH/" build/engine/.libs/lib$i.so >$LIBS/lib$i.so
done

FULLLIBS=$PWD/$LIBS
LIBCC=build
for i in $LIBCC $*
do
    (cd $i; test -d shlibs && cp shlibs/*/.libs/*.so $FULLLIBS)
    for j in $LIBCCNAMED .libs
    do
	for k in $(cd $i/$j; echo *.so)
	do
	    cp $i/$j/$k $LIBS
	done
    done
    shift
done
strip $LIBS/*.so

#copy resources

for i in $EXTRADIRS
do
    test -d $i/res && (cd $i/res; tar cf - .) | (cd res; tar xf -)
    test -d $i/src && (cd $i/src; tar cf - .) | (cd src; tar xf -)
done

#ant debug
ant release || exit 1
cp bin/Gforth-release.apk bin/Gforth.apk
cp bin/Gforth-release.apk bin/Gforth-$(date +%Y%m%d).apk
