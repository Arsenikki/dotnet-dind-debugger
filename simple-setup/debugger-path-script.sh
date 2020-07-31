#!/bin/bash
#
# --- A script that adds current project root folder to debugger config.

script_location=$(realpath $0)
script_path=$(dirname "${script_location}")
project_path=$(realpath "${script_path}/..")

shortenedPath=$(echo $project_path | sed 's|^/[^/]*||')

# --- Replaces path placeholder in debugger.json with current project root folder.
sed -i -e "s:<enter_project_path_here>:$shortenedPath:" "${script_path}/debugger.json"

echo "Project path changed in debugger.json to:"
echo $shortenedPath

# --- Finished
exit 0
