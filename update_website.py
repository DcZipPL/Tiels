# Update website to match with current app version.
import io

r = open("./index.html", "r")
print("Website updater 1.0")
new_version = input("Enter new version: ")
code = r.read()
r.close()
w = open("./index.html", "w")
open_position = code.find("<autogen>")+9
close_positon = code.find("</autogen>")
w.write(code[:open_position]+new_version+"v"+code[close_positon:])
w.close()

r = open("./index.html", "r")
code = r.read()
r.close()
w = open("./index.html", "w")
open_position = code.find("https://github.com/DcZipPL/Tiels/releases/download/")+51
close_positon = code.find(".zip'\"><i class=\"far fa-file-archive\"></i> Direct Download:")
w.write(code[:open_position]+"v"+new_version.replace(".","-")+"-beta/Tiels_"+code[close_positon:])
w.close()