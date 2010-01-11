AC_INIT(fsgateway/FsGateway.cs)

AC_PREREQ([2.13])

VERSION=0.0.3

AM_INIT_AUTOMAKE(fsgateway, $VERSION)

AM_MAINTAINER_MODE

AC_PROG_INSTALL

AC_PATH_PROG(PKG_CONFIG, pkg-config, no)
if test "x$PKG_CONFIG" = "xno"; then
        AC_MSG_ERROR([You need to install pkg-config])
fi
AC_SUBST(PKH_CONFIG)

MONO_REQ_VERSION=2.0.1
PKG_CHECK_MODULES(MONO, mono >= $MONO_REQ_VERSION)

MONO_FUSE_REQ_VERSION=0.4
PKG_CHECK_MODULES([MONO_FUSE], [mono-fuse] >= $MONO_FUSE_REQ_VERSION)

AC_PATH_PROG(GMCS, gmcs, no)
if test "x$GMCS" = "xno"; then
        AC_MSG_ERROR([You need to install gmcs])
fi
AC_SUBST(GMCS)

AC_PATH_PROG(GACUTIL, gacutil, no)
if test "x$GACUTIL" = "xno"; then
        AC_MSG_ERROR([You need to install gacutil])
fi
AC_SUBST(GACUTIL)

dnl find assembly paths for unstable installed assemblies that will be
dnl copied into monoskel installation directory for local usage with the app
UNSTABLE_INSTALL_ASSEMBLIES="mono-fuse"
INSTALLED_ASSEMBLIES="`$PKG_CONFIG --variable=Libraries $UNSTABLE_INSTALL_ASSEMBLIES`"
AC_SUBST(INSTALLED_ASSEMBLIES)

AC_CONFIG_FILES([
fsgateway/Makefile
Makefile
])

AC_OUTPUT
