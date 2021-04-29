# lmpwa 
**(Now on developing)**

Make gas hydrate atomic type lmp (lammps data file) to full type and complete the bonds and angles data.


# Usage
```powershell
lmpwa [options] <SourceFile> <TargetFile>

Arguments:
  SourceFile                   The source lammps data file. (source.lmp)
  TargetFile                   The lammps data file with angle dat. (result.lmp)

Options:
  -h|--help                    Show help information.
  --read-mode                  Determine read source file mode. (atomic)
  -wm|--water-model            Water model name will be used. (SPC)
  -l|--large27                 Determine whether large 27 times. (false)
  -rsa|--remove-surface-angle  Determine whether remove surface angle. (false)
  -fia|--fix-invalid-axis      Determine whether fix invalid axis. (false)
```





