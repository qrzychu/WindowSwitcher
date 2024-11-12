TARGET = WindowsApiWrapper.dll
SRC = WindowsApiWrapper.cpp

all: $(TARGET)

$(TARGET): $(SRC)
	clang++ -shared -lole32 -loleaut32 -lgdiplus -luser32 -lgdi32 -lshell32 -o $(TARGET) $(SRC)

clean:
	del /Q $(TARGET) *.exp *.lib *.o *.obj

.PHONY: all

# Debugging information
debug:
	@echo Current directory: $(CURDIR)
	@echo Source file: $(SRC)
	@echo Target: $(TARGET)
	@dir

.PHONY: debug