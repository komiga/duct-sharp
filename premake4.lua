-- duct# premake file

local name="ductsharp"
local outpath="out/"
local debugpath=outpath.."debug/"..name
local releasepath=outpath.."release/"..name

if _ACTION == "clean" then
	os.rmdir(outpath)
end

solution("ductsharp")
	configurations {"debug", "release"}

local proj=project(name)
proj.language="C#"
proj.kind="SharedLib"

configuration {"debug"}
	targetdir(outpath.."debug/")
	objdir(outpath.."obj/")
	flags {"Symbols", "ExtraWarnings"}
	buildoptions {"-debug"}

configuration {"release"}
	targetdir(outpath.."release/")
	objdir(outpath.."obj/")
	flags {"Optimize", "ExtraWarnings"}

configuration {"gmake"}
	framework("3.0")

configuration {"debug"}
	postbuildcommands {"mkdir -p lib/debug"}
	postbuildcommands {"cp "..debugpath..".dll lib/debug/"..name..".dll"}
	postbuildcommands {"cp "..debugpath..".dll.mdb lib/debug/"..name..".dll.mdb"}

configuration {"release"}
	postbuildcommands {"mkdir -p lib/release"}
	postbuildcommands {"cp "..releasepath..".dll lib/release/"..name..".dll"}

configuration {}

files {"src/*.cs"}

