assemblydir		= $(libdir)

assembly_DATA		= bsio.dll

bsio_dll_SRCS		= stream.sa \
			  out.sa

EXTRA_DIST		= $(bsio_dll_SRCS)

CLEANFILES		= bsio.dll

bsio.dll: $(bsio_dll_SRCS)
	MONO_PATH=$(top_builddir)/src:$(top_builddir)/lib/base \
		$(MONO) $(top_builddir)/src/bsc.exe \
		-target:library \
		-out:bsio.dll $(bsio_dll_SRCS)
