bin_SCRIPTS	= bsc

EXTRA_DIST	= bsc.in

CLEANFILES	= bsc

bsc: bsc.in
	sed -e 's!%MONO%!$(MONO)!g' \
	    -e 's!%bindir%!$(bindir)!g' \
	    $(srcdir)/bsc.in > bsc
	chmod +x bsc
	chmod -w bsc
