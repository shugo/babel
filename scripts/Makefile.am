bin_SCRIPTS	= babelc

EXTRA_DIST	= babelc.in

CLEANFILES	= babelc

babelc: babelc.in
	rm -f babelc
	sed -e 's!%MONO%!$(MONO)!g' \
	    -e 's!%bindir%!$(bindir)!g' \
	    $(srcdir)/babelc.in > babelc
	chmod +x babelc
	chmod -w babelc
