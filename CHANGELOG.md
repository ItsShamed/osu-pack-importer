### What's new?

* __If you run the executable without arguments (e.g: from the file explorer), you'll be guided on providing
the arguments it needs via terminal prompts.__
* The program now checks for duplicate collections and will ask you if you want to continue if it finds one.
You can also disable this behaviour by passing the `--skip-duplicate-check` flag.
* Backups now include the date in the filename to not overwrite the old ones. 
Be sure to clean up your backups from time to time!



#### Comments

I wanted to do a graphical interface but Rider doesn't support Windows Forms Designer for 
.NET Core and JetBrains doesn't seem to care at all about this even tho it's a highly requested
feature. But I don't want to use Visual Studio because man it looks so old lmao.

Might do a mix of terminal and dialogs later! (until Windows Forms Designer is supported in
Rider)