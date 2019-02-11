has_ios_changes = !git.modified_files.grep(/Toggl\.Daneel/).empty?
has_android_changes = !git.modified_files.grep(/Toggl\.Giskard/).empty?
has_foundation_changes = !git.modified_files.grep(/Toggl\.Foundation/).empty?

pr_number = github.pr_json["number"]

auto_label.set(pr_number, "ios", '#51bdf7') if has_ios_changes
auto_label.set(pr_number, "android", '#A4C639') if has_android_changes
auto_label.set(pr_number, "foundation", '#646490') if has_foundation_changes
