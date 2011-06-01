-- duct# premake file

local name="ductsharp"

if _ACTION == "clean" then
	os.rmdir("obj")
end

solution(name)
	configurations {"debug", "release"}

local proj=project(name)
proj.language="C#"
proj.kind="SharedLib"

configuration {"debug"}
	targetdir("lib/debug/")
	flags {"Symbols", "ExtraWarnings"}
	buildoptions {"-debug"}

configuration {"release"}
	targetdir("lib/release/")
	flags {"Optimize", "ExtraWarnings"}

configuration {"gmake"}
	framework("3.5")

configuration {}

files {
	"src/*.cs"
}

