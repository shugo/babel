assemblydir		= $(libdir)

assembly_DATA		= Babel.IO.dll

Babel_IO_dll_SRCS		= stream.sa \
			  out.sa

EXTRA_DIST		= $(Babel_IO_dll_SRCS) \
			  Babel.IO.dll

CLEANFILES		= Babel.IO.dll

Babel.IO.dll: $(Babel_IO_dll_SRCS)
	MONO_PATH=$(top_builddir)/src \
		$(MONO) $(top_builddir)/src/babelc.exe \
		-nostdlib -lib:$(top_builddir)/lib/base -reference:Babel.Base.dll \
                -target:library -out:Babel.IO.dll \
		$(Babel_IO_dll_SRCS)
