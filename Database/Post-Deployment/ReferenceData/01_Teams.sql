/*

Generated via [dbo].[sp_generate_merge]. To regenerate after dbo.Team changes, run:

    EXEC [dbo].[sp_generate_merge] @table_name = 'Team', @schema = 'dbo', @results_to_text = 0,
    @include_use_db = 0, @nologo = 1, @quiet = 1, @delete_if_not_matched = 0;

ExternalApiCode values are football-data.org team IDs (https://api.football-data.org/v4), fetched
directly from GET /v4/competitions/PL/teams?season=2026 (the 20 clubs confirmed for the 2026/27
Premier League season) plus season=2025 (last season, for Wolves/Burnley/West Ham).

*/

SET NOCOUNT ON

MERGE INTO [dbo].[Team] WITH (SERIALIZABLE) AS [Target]
USING (VALUES
  ('82BB03BA-0368-4836-ACC1-00C861971F69','Curaçao','Curaçao','International/Curacao.png',NULL)
 ,('5671552F-58E6-4942-AAF7-04507ADAD6F9','West Bromwich Albion','West Brom','WestBrom.png',NULL)
 ,('65618234-22A1-4E79-B346-06252F2083EF','Mexico','Mexico','International/Mexico.png',NULL)
 ,('CE34F28D-FBDD-4DFF-8C34-072CA102E1FF','Norway','Norway','International/Europe/Norway.png',NULL)
 ,('7D0FCA8B-D4AB-47AF-AFE7-07AAD767EED5','Watford','Watford','Watford.png',NULL)
 ,('505283BE-035F-410D-8873-085D2029BC99','Finland','Finland','International/Europe/Finland.png',NULL)
 ,('7E10C282-B451-445B-8DE4-0A98DF0B4455','Uruguay','Uruguay','International/Uruguay.png',NULL)
 ,('AA7F74FA-BAD7-4D83-9003-0B191E40C108','Côte d''Ivoire','Côte d''Ivoire','International/IvoryCoast.png',NULL)
 ,('CADC5D8E-5239-45D1-A135-111AB6D6002E','Brighton & Hove Albion','Brighton','Brighton.png','397')
 ,('A3BA2155-77AD-4575-9ACB-12ACEFF5FE83','Morocco','Morocco','International/Morocco.png',NULL)
 ,('7A9B7439-A81C-4384-8B15-180804AB4ABE','Slovenia','Slovenia','International/Europe/Slovenia.png',NULL)
 ,('3B8C770C-A7B2-4082-BA72-181EF1CEA994','West Ham United','West Ham','WestHam.png','563')
 ,('5E80D8EA-5F53-4124-8B45-1941308950D9','Cameroon','Cameroon','International/Cameroon.png',NULL)
 ,('E79AE9EA-777B-43DA-B1BB-1AE655AB4907','Algeria','Algeria','International/Algeria.png',NULL)
 ,('DBF34CBC-5E4B-4E33-A871-1C6928F7206B','Republic of Ireland','Ireland','International/Europe/Ireland.png',NULL)
 ,('C3496F44-36A1-49C0-8057-1C6A16900114','Arsenal','Arsenal','Arsenal.png','57')
 ,('41641780-0723-42F7-98E6-1DC628CFE865','Nigeria','Nigeria','International/Nigeria.png',NULL)
 ,('49374102-D2D9-488F-9C82-1DF3660E8E7E','Blackburn Rovers','Blackburn','Blackburn.png',NULL)
 ,('AF2C4430-EE57-4EE6-AF5E-1EAEE3971325','Newcastle United','Newcastle','Newcastle.png','67')
 ,('C84EAD44-5C7A-410E-BA22-20B0AD614723','Bosnia-Herzegovina','Bosnia','International/Europe/Bosnia.png',NULL)
 ,('5B80317A-FB90-4E96-A2C8-23DC65AD1C49','Wales','Wales','International/Europe/Wales.png',NULL)
 ,('F79B9FA3-FB0B-4ECC-8074-25F47400DB33','Germany','Germany','International/Europe/Germany.png',NULL)
 ,('C0271ADC-6D71-4CE0-A26C-28CB98AE85C1','Chile','Chile','International/Chile.png',NULL)
 ,('AB3581B7-F134-49EB-AEC1-2B6295BFD238','Denmark','Denmark','International/Europe/Denmark.png',NULL)
 ,('588E56A1-161A-42A0-B1F8-2D3E50CA087C','Nottingham Forest','Forest','NottinghamForest.png','351')
 ,('7C4FFE82-4CAF-4BDF-B800-2F1715459F0C','Romania','Romania','International/Europe/Romania.png',NULL)
 ,('AC4A1179-8629-47F3-939A-39BD1E1F645E','Georgia','Georgia','International/Europe/Georgia.png',NULL)
 ,('DB8988EF-7A38-49D3-ACB0-3B789EF8F357','Australia','Australia','International/Australia.png',NULL)
 ,('7580E8DA-4F9A-4104-8F97-4098C6035BD5','Austria','Austria','International/Europe/Austria.png',NULL)
 ,('828B1745-CD71-4602-9CA7-4680A36DEF4E','Egypt','Egypt','International/Egypt.png',NULL)
 ,('493109E9-0BAC-4959-974E-48A2A9024E84','Reading','Reading','Reading.png',NULL)
 ,('55E9C68F-F928-4418-956A-4CAB5F8D9774','Brentford','Brentford','Brentford.png','402')
 ,('0ADAF15A-8E95-4B18-B274-50485D8910D1','Peru','Peru','International/Peru.png',NULL)
 ,('2BC13DE6-5ED2-4D71-87B0-53CAD5A1A6E0','Honduras','Honduras','International/Honduras.png',NULL)
 ,('11F7EF58-668D-4972-BCD3-583E3BA4B14D','Liverpool','Liverpool','Liverpool.png','64')
 ,('9B018A8B-9A38-4E75-92FF-5BD658F559BF','Sunderland','Sunderland','Sunderland.png','71')
 ,('F1FBA1EE-0740-46F0-A6C7-5EDDB8576DF4','Turkey','Turkey','International/Europe/Turkey.png',NULL)
 ,('B9E136F5-957B-47FF-A806-5F744382A2FE','Sweden','Sweden','International/Europe/Sweden.png',NULL)
 ,('F5623D8C-8498-4EC3-ADCC-60AF0FD85B1E','Albania','Albania','International/Europe/Albania.png',NULL)
 ,('BA818767-67E5-4488-81A1-60D7278CFAC4','Wigan Athletic','Wigan','Wigan.png',NULL)
 ,('9B7B1330-D83A-42ED-9913-61ABD862124F','Crystal Palace','Palace','CrystalPalace.png','354')
 ,('E6CC88C5-C116-4832-AA6F-6363463C5744','Iceland','Iceland','International/Europe/Iceland.png',NULL)
 ,('199762F5-02E3-4A5F-9A4F-64133449DD2C','Northern Ireland','N. Ireland','International/Europe/NorthernIreland.png',NULL)
 ,('452F2CD8-8D76-4FB8-9543-65438CA63B20','Luton Town','Luton','Luton.png',NULL)
 ,('0887860A-33D8-47F5-B1BA-6FB0E619AD7A','Ukraine','Ukraine','International/Europe/Ukraine.png',NULL)
 ,('ACF1E4BB-D2FE-44DE-8C85-711B5FCAFD19','South Africa','S. Africa','International/SouthAfrica.png',NULL)
 ,('E0E79506-EDB1-4030-97C9-712DD9DEF22D','Belgium','Belgium','International/Europe/Belgium.png',NULL)
 ,('435C85FF-B2D3-4255-83A5-72F0A6AFBE8E','Southampton','Southampton','Southampton.png',NULL)
 ,('64311FFC-2491-4173-B8EC-7432FA9BF73E','Panama','Panama','International/Panama.png',NULL)
 ,('4E73D425-20D0-46E9-9EEB-75A2D932C1D0','Wolverhampton Wanderers','Wolves','Wolves.png','76')
 ,('ABA260FE-28DA-4844-A839-76B91D33C6CC','Huddersfield Town','Huddersfield','Huddersfield.png',NULL)
 ,('5DEA7410-C53C-40D4-87D1-76D2C4971682','Greece','Greece','International/Europe/Greece.png',NULL)
 ,('B9EDEC0B-8C7B-4DB0-B860-792451234248','Czech Republic','Czechia','International/Europe/CzechRepublic.png',NULL)
 ,('BAF0F57C-1738-4525-9FCD-7E27DE841FF6','Iran','Iran','International/Iran.png',NULL)
 ,('105C2C71-BC09-4C5B-A9FE-7E7E97879CB3','Colombia','Colombia','International/Colombia.png',NULL)
 ,('63EC80F7-70E1-4D01-B3C9-7E7FA550E9EE','Queens Park Rangers','QPR','QPR.png',NULL)
 ,('DDF3EB0A-0C6D-43F5-AAA7-7FA799E70139','South Korea','S. Korea','International/SouthKorea.png',NULL)
 ,('A45DED94-1B5F-4831-ACD7-812BA49387E9','Jordan','Jordan','International/Jordan.png',NULL)
 ,('401D157B-BCC8-4DA9-83CF-81787689BD0E','Sheffield United','Sheff Utd','SheffUtd.png',NULL)
 ,('C564D969-8CAC-4EFF-B572-8580CFC08721','Tunisia','Tunisia','International/Tunisia.png',NULL)
 ,('CD2D8104-377A-4BE6-8FCB-87196DA4B1C5','Swansea City','Swansea','Swansea.png',NULL)
 ,('8CA815EE-3AD8-4B97-BD31-8B6634B34584','Tottenham Hotspur','Spurs','Spurs.png','73')
 ,('6F8DAA20-9775-4893-B639-8C0DC74DB0BE','Saudi Arabia','S. Arabia','International/SaudiArabia.png',NULL)
 ,('412D17E1-193C-4523-A1E7-8E8CB55457F0','United States','USA','International/USA.png',NULL)
 ,('0178AEBE-A715-4418-9525-90FF120D290C','Switzerland','Switzerland','International/Europe/Switzerland.png',NULL)
 ,('F71F5EC2-F16C-47C1-9A6C-952A6963353B','Argentina','Argentina','International/Argentina.png',NULL)
 ,('352447EE-EFB8-4AFD-AB78-986230D9F593','Croatia','Croatia','International/Europe/Croatia.png',NULL)
 ,('7FCCEAD8-F7E5-424E-BF85-9A4914777F8A','Cardiff City','Cardiff','CardiffCity.png',NULL)
 ,('85AF461D-5C02-4BD5-948E-9D239925F8A0','Uzbekistan','Uzbekistan','International/Uzbekistan.png',NULL)
 ,('7606A741-A64F-41F2-BA0E-A3ECDAF911DE','Manchester United','Man Utd','ManUtd.png','66')
 ,('36A37659-C418-4E80-B403-A40CE7391B0D','Ghana','Ghana','International/Ghana.png',NULL)
 ,('E0CA03EF-53E9-4E49-977A-A66207C3502C','Cabo Verde','Cabo Verde','International/CapeVerde.png',NULL)
 ,('6E71EB7F-53D5-493B-A268-A80B39676AAB','Brazil','Brazil','International/Brazil.png',NULL)
 ,('E4A50D3C-3CE9-4AB4-8B63-A9E503AA3A69','New Zealand','N. Zealand','International/NewZealand.png',NULL)
 ,('88B89EDB-8804-4873-9D05-AA6B2C6B1A29','Italy','Italy','International/Europe/Italy.png',NULL)
 ,('0C5DC135-4786-44EC-B45D-AC79D0C37D58','Manchester City','Man City','ManCity.png','65')
 ,('8A32C156-37DD-46D2-B07A-AD063C76504A','Costa Rica','Costa Rica','International/CostaRica.png',NULL)
 ,('58FBAA96-F512-471C-93AE-B0781C00DC8B','Burnley','Burnley','Burnley.png','328')
 ,('814C1576-4DB6-4C17-B7B6-B13E8E2D6899','Bolton Wanderers','Bolton','Bolton.png',NULL)
 ,('ABCCE11D-689F-4011-A168-B18132875B21','Leicester City','Leicester','Leicester.png',NULL)
 ,('68253630-5EBE-47F9-9E38-B1ABA1CF018A','Canada','Canada','International/Canada.png',NULL)
 ,('B59794C4-5290-4F60-B409-B1CA1FE658C6','Haiti','Haiti','International/Haiti.png',NULL)
 ,('484496EF-F588-428C-BCF5-B88226B19970','Paraguay','Paraguay','International/Paraguay.png',NULL)
 ,('845D6D61-EE3E-4CB2-BA8F-B9AC90FA6A89','Ipswich Town','Ipswich','Ipswich.png','349')
 ,('28BD77D1-21F4-40E6-AC5A-B9CA6DF10C76','Bournemouth','Bournemouth','Bournemouth.png','1044')
 ,('1888BEC3-88D5-46D3-8F22-BA16598F92E0','Netherlands','Netherlands','International/Europe/Netherlands.png',NULL)
 ,('F2939A32-9A99-4DDA-AED7-BCCD37767300','Chelsea','Chelsea','Chelsea.png','61')
 ,('630E0125-50F3-423D-83FB-BF1382A30961','Leeds United','Leeds','Leeds.png','341')
 ,('12844A16-368C-49A6-8740-BF14BD72FE96','Ecuador','Ecuador','International/Ecuador.png',NULL)
 ,('E777CECE-FE34-44E4-B0EE-BF70CA3892F4','Portugal','Portugal','International/Europe/Portugal.png',NULL)
 ,('729CA748-2DF6-4238-B641-C835694EB596','Poland','Poland','International/Europe/Poland.png',NULL)
 ,('0AD2B74F-0847-455F-A977-C9336EB93FD9','Senegal','Senegal','International/Senegal.png',NULL)
 ,('3B0ADA76-B4AF-4CF8-8BDC-C95681B93F6C','Iraq','Iraq','International/Iraq.png',NULL)
 ,('7B63E3E7-956A-49A8-8D36-CE0D83304F33','North Macedonia','N. Macedonia','International/Europe/Macedonia.png',NULL)
 ,('12D0D43C-5B65-41C9-BB7E-D3A06EF192F8','Hull City','Hull','HullCity.png','322')
 ,('17EC1B0B-B26F-4C10-9049-D4CCAB64EA06','Japan','Japan','International/Japan.png',NULL)
 ,('1981453A-33B5-4D5B-A23A-D8B2F5F66C95','Everton','Everton','Everton.png','62')
 ,('C8413BD1-53AE-46F3-AF49-D96517C62E0B','Slovakia','Slovakia','International/Europe/Slovakia.png',NULL)
 ,('6AD75FB4-D0F4-4D14-9949-DBC8ED161543','Spain','Spain','International/Europe/Spain.png',NULL)
 ,('DE0368F6-BCF8-4410-B750-DFB220704010','Hungary','Hungary','International/Europe/Hungary.png',NULL)
 ,('8A7E420C-C895-4983-BC60-E119BD475A80','DR Congo','DR Congo','International/DRCongo.png',NULL)
 ,('3F5AD926-903E-414A-83AE-E2291DA42427','Middlesbrough','Middlesbrough','Middlesbrough.png',NULL)
 ,('9B77F055-2F66-4642-9AD8-E3CBCF7E3621','Norwich City','Norwich','Norwich.png',NULL)
 ,('52AAD13D-6B19-48A9-85F0-E927604BE341','Fulham','Fulham','Fulham.png','63')
 ,('B82228E0-1A32-423D-A93D-EC163B6E97FE','France','France','International/Europe/France.png',NULL)
 ,('43964850-311E-4838-A368-ECBF0EF2302A','Russia','Russia','International/Europe/Russia.png',NULL)
 ,('8DC22F7B-26DC-4A1C-9339-EFCCE7FC3DB7','England','England','International/Europe/England.png',NULL)
 ,('E07DBED3-BE13-4BD2-B42D-F39DF97C2B9E','Serbia','Serbia','International/Europe/Serbia.png',NULL)
 ,('A457C5E4-1BEF-4A3C-A2D4-F3D3A27E6E61','Aston Villa','Villa','AstonVilla.png','58')
 ,('FD144C1E-82E5-49B2-AA45-F736B9BC86B0','Stoke City','Stoke','Stoke.png',NULL)
 ,('B5CC3E98-736F-47AF-8615-F82E8D7DAB4B','Qatar','Qatar','International/Qatar.png',NULL)
 ,('4385CAAD-A8D1-4ED1-B1A7-FD41BED6EEC7','Scotland','Scotland','International/Europe/Scotland.png',NULL)
 ,('6EAE353E-06EF-4627-9AA7-9DE142600948','Coventry City','Coventry','Coventry.png','1076')
) AS [Source] ([TeamID],[TeamName],[ShortName],[ImageName],[ExternalApiCode])
ON ([Target].[TeamID] = [Source].[TeamID])
WHEN MATCHED AND EXISTS (SELECT [Source].[TeamName], [Source].[ShortName], [Source].[ImageName], [Source].[ExternalApiCode]
                 EXCEPT  SELECT [Target].[TeamName], [Target].[ShortName], [Target].[ImageName], [Target].[ExternalApiCode]) THEN
 UPDATE SET
  [Target].[TeamName] = [Source].[TeamName],
  [Target].[ShortName] = [Source].[ShortName],
  [Target].[ImageName] = [Source].[ImageName],
  [Target].[ExternalApiCode] = [Source].[ExternalApiCode]
WHEN NOT MATCHED BY TARGET THEN
 INSERT([TeamID],[TeamName],[ShortName],[ImageName],[ExternalApiCode])
 VALUES([Source].[TeamID],[Source].[TeamName],[Source].[ShortName],[Source].[ImageName],[Source].[ExternalApiCode]);

SET NOCOUNT OFF
