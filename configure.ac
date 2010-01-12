AC_INIT(src/FsGateway.cs)
AC_CANONICAL_SYSTEM
AM_INIT_AUTOMAKE(fsgateway, 0.0.3)
AM_MAINTAINER_MODE

AC_PROG_INSTALL

dnl pkg-config
AC_PATH_PROG(PKG_CONFIG, pkg-config, no)
if test "x$PKG_CONFIG" = "xno"; then
	AC_MSG_ERROR([You need to install pkg-config])
fi

dnl --- Check for mono and gmcs

AC_PATH_PROG(MONO, mono)
AC_PATH_PROG(MCS, gmcs)

CS="C#"
if test "x$CSC" = "xno" -a "x$MCS" = "xno"  ; then
	dnl AC_MSG_ERROR([You need to install a C# compiler])
	AC_MSG_ERROR([No $CS compiler found])
fi

if test "x$MCS" = "xno" ; then
	MCS=$CSC
fi

if test "x$MONO" = "xno"; then
	AC_MSG_ERROR([No mono runtime found])
else
        PKG_CHECK_MODULES(MONO, mono >= 2.0.1)
fi

AC_SUBST(MCS)

dnl mono-fuse
PKG_CHECK_MODULES(MONO_FUSE, mono-fuse >= 0.4)

dnl mysql-connector-net
PKG_CHECK_MODULES(MYSQL_CONNECTOR_NET, mysql-connector-net)

dnl find assembly paths for unstable installed assemblies that will be
dnl copied into monoskel installation directory for local usage with the app
UNSTABLE_INSTALL_ASSEMBLIES="mono-fuse"
INSTALLED_ASSEMBLIES="`$PKG_CONFIG --variable=Libraries $UNSTABLE_INSTALL_ASSEMBLIES`"
AC_SUBST(INSTALLED_ASSEMBLIES)

AC_OUTPUT([
src/Makefile
Makefile
])

dnl Configuration summary
echo ""
echo "Configuration summary"
echo ""
echo "   * Installation prefix = $prefix"
echo "   * $CS compiler: $MCS"
echo ""
