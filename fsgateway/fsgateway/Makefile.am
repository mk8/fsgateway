
EXTRA_DIST =  

# Warning: This is an automatically generated file, do not edit!

if ENABLE_DEBUG
ASSEMBLY_COMPILER_COMMAND = gmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -optimize+ -debug "-define:DEBUG"
 "-main:FsGateway.FsGateway"

ASSEMBLY = ../bin/fsgateway.exe
ASSEMBLY_MDB = $(ASSEMBLY).mdb
COMPILE_TARGET = exe
PROJECT_REFERENCES = 
BUILD_DIR = ../bin

MONO_FUSE_DLL_SOURCE=../../../../../../usr/lib/mono-fuse/Mono.Fuse.dll
MONO_FUSE_DLL_CONFIG_SOURCE=../../../../../../usr/lib/mono-fuse/Mono.Fuse.dll.config

endif

if ENABLE_RELEASE
ASSEMBLY_COMPILER_COMMAND = gmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -optimize+ "-main:FsGateway.FsGateway"

ASSEMBLY = bin/Release/fsgateway.exe
ASSEMBLY_MDB = 
COMPILE_TARGET = exe
PROJECT_REFERENCES = 
BUILD_DIR = bin/Release

MONO_FUSE_DLL_SOURCE=../../../../../../usr/lib/mono-fuse/Mono.Fuse.dll
MONO_FUSE_DLL_CONFIG_SOURCE=../../../../../../usr/lib/mono-fuse/Mono.Fuse.dll.config

endif

AL=al2
SATELLITE_ASSEMBLY_NAME=.resources.dll

PROGRAMFILES = \
	$(MONO_FUSE_DLL) \
	$(MONO_FUSE_DLL_CONFIG)  

BINARIES = \
	$(FSGATEWAY)  


	
all: $(ASSEMBLY) $(PROGRAMFILES) $(BINARIES) 

FILES =  \
	AssemblyInfo.cs \
	DirAsTag.cs \
	Field.cs \
	FsDbManager.cs \
	FsGateway.cs \
	FuseWrapper.cs \
	IFsDb.cs \
	IFsGateway.cs \
	Index.cs \
	Postgresql.cs \
	Sequence.cs \
	SugarDatastore.cs \
	Table.cs \
	View.cs 

DATA_FILES = 

RESOURCES = 

EXTRAS = \
	fsgateway.in 

REFERENCES =  \
	Mono.Posix \
	Npgsql \
	System \
	System.Data \
	usr/lib/mono-fuse/Mono.Fuse.dll

DLL_REFERENCES =  \
	../../../../../../usr/lib/mono-fuse/Mono.Fuse.dll

CLEANFILES = $(PROGRAMFILES) $(BINARIES) 

include $(top_srcdir)/Makefile.include

FSGATEWAY = $(BUILD_DIR)/fsgateway
MONO_FUSE_DLL = $(BUILD_DIR)/Mono.Fuse.dll
MONO_FUSE_DLL_CONFIG = $(BUILD_DIR)/Mono.Fuse.dll.config

$(eval $(call emit-deploy-wrapper,FSGATEWAY,fsgateway,x))
$(eval $(call emit-deploy-target,MONO_FUSE_DLL))
$(eval $(call emit-deploy-target,MONO_FUSE_DLL_CONFIG))


$(build_xamlg_list): %.xaml.g.cs: %.xaml
	xamlg '$<'

$(build_resx_resources) : %.resources: %.resx
	resgen2 '$<' '$@'

$(ASSEMBLY) $(ASSEMBLY_MDB): $(build_sources) $(build_resources) $(build_datafiles) $(DLL_REFERENCES) $(PROJECT_REFERENCES) $(build_xamlg_list) $(build_satellite_assembly_list)
	mkdir -p $(dir $(ASSEMBLY))
	$(ASSEMBLY_COMPILER_COMMAND) $(ASSEMBLY_COMPILER_FLAGS) -out:$(ASSEMBLY) -target:$(COMPILE_TARGET) $(build_sources_embed) $(build_resources_embed) $(build_references_ref)
