SUBDIRS = src
bin_SCRIPTS = fsgateway
EXTRA_DIST = fsgateway.in

REWRITE = \
	sed -e "s|\@prefix\@|$(prefix)|g"       \
	    -e "s|\@pkglibdir\@|$(pkglibdir)|g" \
	    -e "s|\@bindir\@|$(bindir)|g"		\
	    -e "s|\@MONO\@|$(MONO)|g" 

$(bin_SCRIPTS): $(srcdir)/fsgateway.in Makefile
	$(REWRITE) $(srcdir)/fsgateway.in > $@.tmp
	mv $@.tmp $@

CLEANFILES = \
	fsgateway

MAINTAINERCLEANFILES = \
        INSTALL \
        Makefile.in \
        aclocal.m4 \
        config.guess \
        config.sub \
        configure \
        install-sh \
        missing
