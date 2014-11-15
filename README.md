Archive Unpacker for The Binding of Isaac: Rebirth
=====================

This tool unpacks the archive files (".a") from your The Binding of Isaac: Rebirth installation. You only have to modify the directory in Program.cs, run the program and the magic is being applied on your .a files! Note that it will extract every file to the `output` directory in the working directory of the application (most likely the `bin/Debug` directory under the `TBAR Archive Unpacker` directory).

### Issues
Currently, the unpacker fails to correctly unpack LZW compressed textfiles sometimes...

### Questions

#### Why are all the filenames numbers?
The files are packed inside the archive files with hashes to distinguish themselves. This hashing algorithm is one-way. You can use the hash function in Program.cs to generate the corresponding hash of a filename.
