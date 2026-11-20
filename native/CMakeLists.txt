cmake_minimum_required(VERSION 3.28)

project(DxTools.Native LANGUAGES CXX)

add_library(DxTools.Native SHARED
    src/DxToolsNative.cpp
)

target_include_directories(DxTools.Native PRIVATE include)
target_compile_features(DxTools.Native PRIVATE cxx_std_20)
target_compile_definitions(DxTools.Native PRIVATE UNICODE _UNICODE WIN32_LEAN_AND_MEAN NOMINMAX)

set_target_properties(DxTools.Native PROPERTIES
    PREFIX ""
    OUTPUT_NAME "DxTools.Native"
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
endif()

set(CMAKE_RUNTIME_OUTPUT_DIRECTORY "${CMAKE_BINARY_DIR}/out")
set_target_properties(DxTools.Native PROPERTIES
    RUNTIME_OUTPUT_DIRECTORY "${CMAKE_BINARY_DIR}/out"
    LIBRARY_OUTPUT_DIRECTORY "${CMAKE_BINARY_DIR}/out"
    ARCHIVE_OUTPUT_DIRECTORY "${CMAKE_BINARY_DIR}/out"
)
