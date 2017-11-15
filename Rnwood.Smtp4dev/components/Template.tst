module Api {

	$Classes(c => c.Namespace == "Rnwood.Smtp4dev.DbModel")[ 
		export class $Name$TypeParameters { $Properties(p => !p.Attributes.Any(a => a.Name.Contains("JsonIgnoreAttribute")))[
			$name: $Type;]
		}]

}

