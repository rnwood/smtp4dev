${
    using Typewriter.Extensions.WebApi;
 
    string ReturnType(Method m) => m.Type.Name;
    string ServiceName(Class c) => c.Name;

    string Imports(Class c){
        List<string> neededImports = c.Properties
	        .Where(p => !p.Type.IsPrimitive)
	        .Select(p => "import " + p.Type.Name.TrimEnd('[',']') + " from './" + p.Type.Name.TrimEnd('[',']') + "';").ToList();
        if (c.BaseClass != null) { 
	        neededImports.Add("import " + c.BaseClass.Name +" from './" + c.BaseClass.Name + "';");
        }
        return String.Join("\n", neededImports.Distinct());
    }

    string ControllerImports(Class c){
        List<string> neededImports = c.Methods
	        .Where(m => !m.Type.IsPrimitive && !m.Type.Name.Contains("void"))
	        .Select(p => "import " + p.Type.Name.TrimEnd('[',']') + " from './" + p.Type.Name.TrimEnd('[',']') + "';").ToList();
        return String.Join("\n", neededImports.Distinct());
    } 
}
$Classes(c => c.Namespace == "Rnwood.Smtp4dev.ApiModel")[$Imports
export default class $Name$TypeParameters { $Properties(p => !p.Attributes.Any(a => a.Name.Contains("JsonIgnoreAttribute")))[ 
    $name: $Type;]
}]
$Classes(*Controller)[$ControllerImports
import axios from "axios";

export default class $ServiceName {
    public _baseUrl: string;                
 
    constructor(baseUrl: string = "/"){
        this._baseUrl = baseUrl;
    }
        
    $Methods[
    // $HttpMethod: $Url       
    public async $name($Parameters[$name: $Type][, ]): Promise<$ReturnType> {
        let route = ($Parameters(p => p.Type.IsPrimitive)[$name: $Type][, ]) => `${this._baseUrl}$Url`;

        return (await axios.$HttpMethod(route($Parameters(p => p.Type.IsPrimitive)[$name][, ]), $RequestData || undefined)).data as $ReturnType;
    }]
}]