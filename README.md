## 使用donet sdk 8.0.111 + vscode 
### 项目配置 在*.csproj
```
  <!-- 添加本地DLL文件 -->
  <ItemGroup>
    <Reference Include="Assembly-CSharp">
      <HintPath>./lib/Assembly-CSharp.dll</HintPath>
    </Reference>
  </ItemGroup>

    <!-- 生成的位置 -->
  <PropertyGroup>
    <OutputPath>/home/soulcoco/.local/share/Steam/steamapps/common/Richman11/BepInEx/plugins/</OutputPath> 这里改你要生成的路径
  </PropertyGroup>
```
## 使用  具体请查看BepInEx官网文档
```
dotnet new bepinex5plugin -n MonopolyCardModifier //创建项目
dotnet restore MonopolyCardModifier
dotnet add package HarmonyX //添加HarmonyX
```
