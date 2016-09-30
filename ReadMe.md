Roslyn Analyzer used to generate manual technical debt in Sonarqube.

Using [SonarSource-VisualStudio/sonarqube-roslyn-sdk](https://github.com/SonarSource-VisualStudio/sonarqube-roslyn-sdk), you can generate a plugin to sonarqube that will scan C# source code for any ManualTechnicalDebt attribute in code and will create an issue in sonarqube with resolution time according to a parameter in the attribute.

Exemple : 

```c#
[ManualTechnicalDebt("Debt sample", SqaleRemediationDaysEffort = 50)]
public class Class1
{
}
```

Custom attribute to define : 
```c#
internal class ManualTechnicalDebtAttribute : Attribute
{
    private int _sqaleRemediationDaysEffort;
    private string _technicalDebtInfo;

    public ManualTechnicalDebtAttribute(string technicalDebtInfo)
    {
        this._technicalDebtInfo = technicalDebtInfo;
    }

    public int SqaleRemediationDaysEffort
    {
        get
        {
            return _sqaleRemediationDaysEffort;
        }

        set
        {
            _sqaleRemediationDaysEffort = value;
        }
    }
}
```
