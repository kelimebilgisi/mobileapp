modified_files = git.modified_files + git.added_files

has_ios_changes = !modified_files.grep(/Toggl\.Daneel/).empty?
has_android_changes = !modified_files.grep(/Toggl\.Giskard/).empty?
has_ui_test_changes = !modified_files.grep(/Tests\.UI/).empty?
has_foundation_changes = !modified_files.grep(/Toggl\.Foundation/).empty?
has_sync_changes = !modified_files.grep(/Sync/).empty?

pr_number = github.pr_json["number"]

auto_label.set(pr_number, "ios", '#51bdf7') if has_ios_changes
auto_label.set(pr_number, "android", '#A4C639') if has_android_changes
auto_label.set(pr_number, "foundation", '#646490') if has_foundation_changes
auto_label.set(pr_number, "ui-tests", '#13c48f') if has_ui_test_changes
auto_label.set(pr_number, "sync", '#9401bc') if has_sync_changes
