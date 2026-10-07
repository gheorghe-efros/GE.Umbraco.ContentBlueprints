import { UMB_SETTINGS_SECTION_ALIAS } from '@umbraco-cms/backoffice/settings';

export const onInit = (_host, extensionRegistry) => {
	// Core's "Document Blueprint for..." create option builds its path with the Settings section
	// hard-coded, and needs document type access that Content-only users do not have. Scope it to
	// Settings, so the Content menu offers folders only; blueprints are created from a document via
	// the "Create Document Blueprint" entity action instead.
	extensionRegistry.appendCondition('Umb.EntityCreateOptionAction.DocumentBlueprint.Default', {
		alias: 'Umb.Condition.SectionAlias',
		match: UMB_SETTINGS_SECTION_ALIAS,
	});

	// Core's blueprint children table links to the Settings section. This package's replacement
	// links to whichever section it is rendered in, so it replaces core's in both sections.
	extensionRegistry.exclude('Umb.CollectionView.DocumentBlueprint.TreeItem.Table');
};
