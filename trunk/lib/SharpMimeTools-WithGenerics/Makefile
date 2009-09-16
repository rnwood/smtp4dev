CSC=mcs
# CSC=gmcs
CSCFLAGS+= /define:MONO /warn:4 /nologo /target:library

# Mono version (used only for subdirs)
MONO_VERION = 1.0
# MONO_VERION = 2.0

# Symbols that identify build target
CSCFLAGS+= /define:MONO_1_0 /define:API_1_1
# CSCFLAGS+= /define:MONO_2_0 /define:API_2_0

# Target subdir
TARGET_BUILD = debug
# TARGET_BUILD = release

# Debug compile options
CSCFLAGS+= /debug+ /debug:full /define:DEBUG

# Base dir of SharpMimeTools source
BASE_SMT = .

# Base dir for references (log4net & FCKeditor assemblies should be there)
BASE_REF = ../mono
BASE_REF_VERSION = ../mono/$(MONO_VERION)

BIN_SMT = $(BASE_SMT)/bin/mono/$(MONO_VERION)/$(TARGET_BUILD)

SOURCES_SMT = $(BASE_SMT)/src/*.cs

REFERENCES_SMT= System.Web $(BASE_REF)/log4net
REFS_SMT= $(addsuffix .dll, $(addprefix /r:, $(REFERENCES_SMT)))

all: SharpMimeTools

SharpMimeTools: smtdir keypair $(BIN_SMT)/SharpMimeTools.dll

smtdir:
	if [ ! -d $(BIN_SMT) ]; then \
                mkdir -p $(BIN_SMT); \
        fi; \

$(BIN_SMT)/SharpMimeTools.dll: $(SOURCES_SMT)
	$(CSC) $(CSCFLAGS) $(REFS_SMT)  /out:$@ $^

keypair: SharpMimeTools.snk

SharpMimeTools.snk:
        sn -k SharpMimeTools.snk

clean:
	rm -f $(BIN_SMT)/SharpMimeTools.dll

distclean:
	rm -Rf $(BASE_SMT)/bin/
