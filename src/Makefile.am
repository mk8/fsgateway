fsgatewaydir=$(pkglibdir)
fsgateway_SCRIPTS = fsgateway.exe
EXTRA_DIST = $(fsgateway_sources)
CLEANFILES = fsgateway.exe fsgateway.exe.mdb
MAINTAINERCLEANFILES = Makefile.in

fsgateway_sources = AssemblyInfo.cs \
	DirAsTag.cs \
	Field.cs \
	FsDbManager.cs \
	FsGateway.cs \
	FsXml.cs \
	FuseWrapper.cs \
	IFsDb.cs \
	IFsGateway.cs \
	IFsModule.cs \
	Index.cs \
	OlpcMetadata.cs \
	Postgresql.cs \
	Sequence.cs \
	Function.cs \
	SugarDatastore.cs \
	Table.cs \
	View.cs \
	FsMySql.cs \
	FsSqlServer.cs \
	FsSqlite.cs

fsgateway_libs = \
	-r:System \
	-r:System.Data \
	-r:Npgsql \
	-r:Mono.Posix \
	-r:System.Xml \
	-r:Mono.Data.Sqlite \
	$(MYSQL_CONNECTOR_NET_LIBS) \
	$(MONO_FUSE_LIBS)

fsgateway_flags = -noconfig -codepage:utf8 -warn:4 -optimize+ -debug "-define:DEBUG" "-main:FsGateway.FsGateway"

fsgateway.exe: $(fsgateway_sources)
	$(MCS) $(fsgateway_flags) $(fsgateway_libs) -out:$@ $(fsgateway_sources)


# Install Unstable Mono Libraries (see configure.ac)

install-data-hook:
	for ASM in $(INSTALLED_ASSEMBLIES); do \
		$(INSTALL) -c -m 0755 $$ASM $(DESTDIR)$(pkglibdir); \
	done;

uninstall-hook:
	for ASM in $(INSTALLED_ASSEMBLIES); do \
		rm -f $(DESTDIR)$(pkglibdir)/`basename $$ASM`; \
	done;

