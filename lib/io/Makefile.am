assemblydir		= $(libdir)

assembly_DATA		= bsio.dll

bsio_dll_SRCS		= stream.sa \
			  out.sa

EXTRA_DIST		= $(bsio_dll_SRCS) \
			  bsio.dll

CLEANFILES		= bsio.dll

bsio.dll: $(bsio_dll_SRCS)
	MONO_PATH=$(top_builddir)/src \
		$(MONO) $(top_builddir)/src/babelc.exe \
		-nostdlib -lib:$(top_builddir)/lib/base -reference:bsbase.dll \
                -target:library -out:bsio.dll \
		$(bsio_dll_SRCS)
