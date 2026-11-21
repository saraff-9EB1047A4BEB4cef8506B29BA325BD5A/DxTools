cmake_minimum_required(VERSION 3.28)

project(DxTools.Native LANGUAGES CXX)

add_library(DxTools.Native SHARED
    src/DxToolsNative.cpp
)

target_include_directories(DxTools.Native PRIVATE include)
target_compile_features(DxTools.Native PRIVATE cxx_std_20)
target_compile_definitions(DxTools.Native PRIVATE UNICODE _UNICODE WIN32_LEAN_AND_MEAN NOMINMAX)

target_link_libraries(DxTools.Native PRIVATE ole32 oleaut32 propsys uuid)

set_target_properties(DxTools.Native PROPERTIES
    PREFIX ""
    OUTPUT_NAME "DxTools.Native"
    MSVC_RUNTIME_LIBRARY "MultiThreaded$<$<CONFIG:Debug>:Debug>"
)

add_library(DxTools.ShellExt SHARED
    shellext/DxToolsShellExt.cpp
)

target_compile_features(DxTools.ShellExt PRIVATE cxx_std_20)
target_compile_definitions(DxTools.ShellExt PRIVATE UNICODE _UNICODE WIN32_LEAN_AND_MEAN NOMINMAX _WIN32_WINNT=0x0A00 WINVER=0x0A00)

target_link_libraries(DxTools.ShellExt PRIVATE
    advapi32
    ole32
    shell32
    shlwapi
    user32
    uuid
)

set_target_properties(DxTools.ShellExt PROPERTIES
    PREFIX ""
    OUTPUT_NAME "DxTools.ShellExt"
    MSVC_RUNTIME_LIBRARY "MultiThreaded$<$<CONFIG:Debug>:Debug>"
)

if(MSVC)
    target_compile_options(DxTools.Native PRIVATE
        /W4
        /permissive-
        /EHsc
        /guard:cf
        /sdl
    )
    target_link_options(DxTools.Native PRIVATE
        /DYNAMICBASE
        /HIGHENTROPYVA
        /NXCOMPAT
        /guard:cf
    )
    target_compile_options(DxTools.ShellExt PRIVATE
        /W4
        /permissive-
        /EHsc
        /guard:cf
        /sdl
    )
    target_link_options(DxTools.ShellExt PRIVATE
        /DYNAMICBASE
        /HIGHENTROPYVA
        /NXCOMPAT
        /guard:cf
    )
else()
    target_compile_options(DxTools.Native PRIVATE
        -Wall
        -Wextra
        -Werror
        -fno-exceptions
        -fno-rtti
    )
    target_link_options(DxTools.Native PRIVATE
        -Wl,--no-undefined
    )
    target_compile_options(DxTools.ShellExt PRIVATE
        -Wall
        -Wextra
        -Werror
        -fno-exceptions
        -fno-rtti
    )
    target_link_options(DxTools.ShellExt PRIVATE
        -Wl,--no-undefined
    )
endif()

set(CMAKE_RUNTIME_OUTPUT_DIRECTORY "${CMAKE_BINARY_DIR}/out")
set_target_properties(DxTools.Native DxTools.ShellExt PROPERTIES
    RUNTIME_OUTPUT_DIRECTORY "${CMAKE_BINARY_DIR}/out"
    LIBRARY_OUTPUT_DIRECTORY "${CMAKE_BINARY_DIR}/out"
    ARCHIVE_OUTPUT_DIRECTORY "${CMAKE_BINARY_DIR}/out"
)
