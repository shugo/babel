assemblydir		= $(libdir)

assembly_DATA		= bsbase.dll

bsbase_dll_SRCS		= abstract.sa

EXTRA_DIST		= $(bsbase_dll_SRCS)

CLEANFILES		= bsbase.dll

bsbase.dll: $(bsbase_dll_SRCS)
	MONO_PATH=$(top_builddir)/src \
		$(MONO) $(top_builddir)/src/bsc.exe \
		-nostdlib -target:library -out:bsbase.dll \
		$(bsbase_dll_SRCS)
