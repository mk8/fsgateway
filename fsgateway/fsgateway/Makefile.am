
ASSEMBLY_COMPILER_COMMAND = gmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -optimize+ -debug "-define:DEBUG"
 "-main:FsGateway.FsGateway"

ASSEMBLY = ../bin/fsgateway.exe
ASSEMBLY_MDB = $(ASSEMBLY).mdb
COMPILE_TARGET = exe
PROJECT_REFERENCES = 
BUILD_DIR = ../bin

BINARIES = \
	$(FSGATEWAY) 

LIBFLAGS = \
	-r:System \
	-r:System.Data \
	-r:Npgsql \
	-r:Mono.Posix \
	-r:System.Xml \
	-pkg:mysql-connector-net \
	-pkg:mono-fuse


all: $(BINARIES) 

FILES =  \
	AssemblyInfo.cs \
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
	SugarDatastore.cs \
	Table.cs \
	View.cs \
	FsMySql.cs


DATA_FILES = 

RESOURCES = 

EXTRAS = \
	fsgateway.in 


CLEANFILES = $(PROGRAMFILES) $(BINARIES) 

FSGATEWAY = $(ASSEMBLY)

MAIN = FsGateway.FsGateway

$(eval $(call emit-deploy-wrapper,FSGATEWAY,fsgateway,x))

$(ASSEMBLY) $(ASSEMBLY_MDB): $(PROJECT_REFERENCES) $(FILES)
	mkdir -p $(dir $(ASSEMBLY))
	$(ASSEMBLY_COMPILER_COMMAND) $(ASSEMBLY_COMPILER_FLAGS) -out:$(ASSEMBLY) -target:$(COMPILE_TARGET) -main:$(MAIN) $(LIBFLAGS) $(FILES)
