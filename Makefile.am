SUBDIRS	= src \
	  lib \
	  scripts \
	  tests

EXTRA_DIST = README.ja

release: dist
	cp $(distdir).tar.gz /home/shugo/www/www.shugo.net/babel/
	cd /home/shugo/www/www.shugo.net/ && make install
