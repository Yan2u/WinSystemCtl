cmake_minimum_required(VERSION 3.16)

project(WinSystemCtl.PseudoConsoleCore)

file(GLOB_RECURSE SOURCES src/*.cpp)
file(GLOB_RECURSE HEADERS include/*.h include/*.hpp)
file(GLOB_RECURSE APPS app/*.cpp)
file(GLOB_RECURSE LIBS lib/*.cpp)

set(CMAKE_EXPORT_COMPILE_COMMANDS ON)
set(CMAKE_CXX_STANDARD 20)

# for debug build, add /Zi /Od), add macro DEBUG
if (CMAKE_BUILD_TYPE MATCHES Debug)
    add_compile_options(/Od)
    add_definitions(-DDEBUG)
    set(DLL_INSTALL_PATH "${CMAKE_CURRENT_SOURCE_DIR}\\..\\WinSystemCtl\\bin\\x64\\Debug\\net8.0-windows10.0.19041.0")
endif()

# for release build
if (CMAKE_BUILD_TYPE MATCHES Release)
    add_compile_options(/O2 /MD /GS-)
    set(DLL_INSTALL_PATH "${CMAKE_CURRENT_SOURCE_DIR}\\..\\WinSystemCtl\\bin\\x64\\Release\\net8.0-windows10.0.19041.0")
endif()

message(STATUS "SOURCES=${SOURCES}")
message(STATUS "HEADERS=${HEADERS}")
message(STATUS "APPS=${APPS}")
message(STATUS "LIBS=${LIBS}")

include_directories(include)

foreach(app ${APPS})
    get_filename_component(source_name ${app} NAME)
    string(REPLACE ".cpp" "" source_name_no_ext ${source_name})
    add_executable(${source_name_no_ext} ${app} ${SOURCES})
    target_link_libraries(${source_name_no_ext} PRIVATE kernel32.lib)
    message(STATUS "APP Added ${source_name_no_ext}")
endforeach()

foreach(lib ${LIBS})
    get_filename_component(source_name ${lib} NAME)
    string(REPLACE ".cpp" "" source_name_no_ext ${source_name})
    add_library(${source_name_no_ext} SHARED ${lib} ${SOURCES})
    target_link_libraries(${source_name_no_ext} PRIVATE kernel32.lib)
    message(STATUS "LIB Added ${source_name_no_ext}")

    add_custom_command(TARGET ${source_name_no_ext}
        POST_BUILD
        COMMAND ${CMAKE_COMMAND} -E copy $<TARGET_FILE:${source_name_no_ext}> ${DLL_INSTALL_PATH}
        COMMENT "Copying ${source_name_no_ext} to ${DLL_INSTALL_PATH}"
    )
endforeach(lib ${LIBS})

