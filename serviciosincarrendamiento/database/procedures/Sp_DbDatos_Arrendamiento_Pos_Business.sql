-- =============================================
-- Author:		MEYLIN VALLECILLOS
-- Create date: 13/04/2016
-- Description:	get comercios a debitar
-- =============================================
CREATE PROCEDURE [dbo].[Sp_DbDatos_Arrendamiento_Pos_Business]-- '2024-08-24'
	-- Add the parameters for the stored procedure here
	@Fecha datetime
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

    -- Insert statements for procedure here

select 
--top 150
		com.retailer,
		com.padre,
		com.monto,
		com.saldo,
		com.mes,
		com.debe,
		com.pos,
		d.id_pag as idPago
		,isnull(f.Iva, '') 'iva'
		,isnull(a.Sic_Code, '') 'mcc'
		,isnull(b.Nombre, '') 'nombre'
		,isnull(b.Direccion, '') 'direccion'
		,isnull(f.Nombre, '') 'bnail'
		,isnull(f.Nit, '') 'nit'
		,isnull(i.Abreviatura, '') 'banco'
		,isnull(c.Cuenta, '') 'cuenta'
		,i.FiidPrincipal [Tercero]		
	    ,i.[ContaCtaAbono] [ContaCtaAbono]
		,i.[ContaCtaArrendamiento] [ContaCtaArrendamiento]
		,i.[ContaCtaAcumulado] [ContaCtaAcumulado]
		,i.[ContaCtaIvaContri] [ContaCtaIvaContri]
		,i.[ContaCtaIvaNoContri] [ContaCtaIvaNoContri]
		,'ARR EQ POS ' + i.Abreviatura + ' ' + convert(varchar(10), @Fecha, 105) [ConceptoAcum]
	from 
		Vw_Arrendamiento_Pos_comercios as com
		inner join dbdatos.dbo.dftdet as d on com.RETAILER = d.rtl_id
		inner join dbdatos.dbo.dftnot as n on d.id_pag= n.id_pag
		LEFT JOIN dbdatos.Comercios.DbDatos_Bussiness_Data b on com.Retailer = b.Retailer
		LEFT JOIN dbdatos.Comercios.DbDatos_Afiliation_Data a on b.IdAfiliacion_Fk = a.Id
		LEFT JOIN dbdatos.Comercios.DbDatos_Fiscal_Data f on a.IdIva_Fk = f.Id
		LEFT JOIN dbdatos.Instituciones.DbDatos_Instituciones i on a.IdInstitucion_Fk = i.Id
		LEFT JOIN dbdatos.Comercios.DbDatos_Cuentas_Bancarias c on c.id = (SELECT top 1 cb.Id from dbdatos.Comercios.DbDatos_Comercio_Relation_Cuentas crc INNER JOIN dbdatos.Comercios.DbDatos_Cuentas_Bancarias cb on crc.IdCuenta_Fk = cb.Id WHERE crc.IdGrupo_Fk = a.Id and crc.Estado =1 and cb.Estado = 1 order by crc.Porcentaje desc)
		 
		 --Validar cuenta primaria
	where 
		--convert(date, d.fecha_gen) = convert(date, @Fecha)
		convert(date, n.fecha_gen) = convert(date, @Fecha, 111)
		and com.estado > 0
		and com.saldo > 0
		and d.estado = 0
	group by 
		com.retailer,
		com.padre,
		com.monto,
		com.saldo,
		com.mes,
		com.debe,
		com.pos,
		d.id_pag 
		,f.Iva
		,a.Sic_Code
		,b.Nombre
		,b.Direccion
		,f.Nombre
		,f.Nit
		,i.Abreviatura
		,c.Cuenta
		,i.FiidPrincipal
		,i.[ContaCtaAbono] 
		,i.[ContaCtaArrendamiento] 
		,i.[ContaCtaAcumulado] 
		,i.[ContaCtaIvaContri] 
		,i.[ContaCtaIvaNoContri] 
	ORDER BY  i.Abreviatura , com.debe desc


END 