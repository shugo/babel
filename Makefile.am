SUBDIRS	= src \
	  lib \
	  scripts \
	  tests

EXTRA_DIST = README.ja

release: dist
	cp $(distdir).tar.gz /home/shugo/www/shugo.net/babell/
	cd /home/shugo/www/shugo.net/ && make install
