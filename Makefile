CONTRIB_SRC = contrib/

APP_SRC = src/
APP_DEST = ${DESTDIR}/usr/share/gcaliper/

#BIN_SRC = contrib/
BIN_DEST = ${DESTDIR}/usr/bin/

#APP_SRC = src/
#APP_DEST = /tmp/make/usr/share/gcaliper/

#BIN_SRC = usr/bin/
#BIN_DEST = /tmp/make/usr/bin/

.PHONY: install
.PHONY: compile

compile:
	xbuild solution.sln

install:
	install -d $(APP_DEST)
	install -d $(APP_DEST)bin
	
	install -m755 $(APP_SRC)bin/gcaliper.exe $(APP_DEST)bin
	install -m644 $(APP_SRC)bin/gcaliper.exe.mdb $(APP_DEST)bin
	install -m644 $(APP_SRC)appicon.ico $(APP_DEST)
	
	install -d $(APP_DEST)themes/caliper
	install -D -m644 $(APP_SRC)themes/caliper/*.png $(APP_DEST)themes/caliper
	install -D -m644 $(APP_SRC)themes/caliper/*.conf $(APP_DEST)themes/caliper

	install -d $(BIN_DEST)
	install -m755 $(CONTRIB_SRC)gcaliper $(BIN_DEST)

	install -d ${DESTDIR}/usr/share/applications
	install -m644 $(CONTRIB_SRC)gcaliper.desktop ${DESTDIR}/usr/share/applications
