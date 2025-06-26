# icu-74-debian
Debian build files for icu 74.2 with a patch to fix CVE-2025-5222

You need to download and extract [icu_74.2.orig.tar.gz](https://snapshot.debian.org/archive/debian/20231215T150950Z/pool/main/i/icu/icu_74.2.orig.tar.gz) to set up your source tree.

Clone this repo somewhere with the following command :-

```
git clone https://github.com/berkley4/icu-74-debian
```

Copy the debian folder from the cloned repo to your icu source directory, eg :-

```
cp -a icu-74-debian/debian icu/
```

Change into the icu source tree and start building the icu deb files :-

```
cd icu

DEB_BUILD_MAINT_OPTIONS=abi=+time64 JOBS=4 dpkg-buildpackage -b -uc -nc
```

End users, eg those who have downloaded my ungoogled chromium debs, need to install :-

libicu74_74.2-1_amd64.deb


Builders need to install the following debs :-

libicu74_74.2-1_amd64.deb
libicu-dev_74.2-1_amd64.deb
icu-devtools_74.2-1_amd64.deb
