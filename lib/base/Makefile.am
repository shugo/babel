assemblydir		= $(libdir)

assembly_DATA		= Babel.Base.dll

Babel_Base_dll_SRCS		= abstract.sa

EXTRA_DIST		= $(Babel_Base_dll_SRCS) \
			  Babel.Base.dll

CLEANFILES		= Babel.Base.dll

Babel.Base.dll: $(Babel_Base_dll_SRCS)
	MONO_PATH=$(top_builddir)/src \
		$(MONO) $(top_builddir)/src/babelc.exe \
		-nostdlib -target:library -out:Babel.Base.dll \
		$(Babel_Base_dll_SRCS)
