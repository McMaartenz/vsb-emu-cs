# Maartanic

[VSB Engine alternative](https://scratch.mit.edu/studios/27769777/)
<p>Is supposed to work with VSB Engine version 1.3. Does not support graphics <b>yet</b>.</p>
<br>
<a href="https://1drv.ms/w/s!AnfmoStjhZY_gYJHHfx08GvbdRHsAg?e=VxucUb">Official VSB Documentation</a>
<p>
Simply compile all C# files and execute the executable. Entry point is inside <kbd>Program.cs</kbd>. Any added argument is used as the file to execute. If not, you will be prompted if you would like to execute the <kbd>autorun.mrt</kbd> file. (We do not include this file.)
</p>

## Modifying the current mode
<p>Write in your code (Needs to be executed, so e.g. put after <kbd>DEF main</kbd>!) <kbd>[mode xyz]</kbd> where <kbd>xyz</kbd> is your preferred mode. <kbd>vsb</kbd> will restrict instruction set to merely VSB's instructions. <kbd>extended</kbd> will extend the VSB's instruction set. (Example: <kbd>[mode extended]</kbd>)</p>
