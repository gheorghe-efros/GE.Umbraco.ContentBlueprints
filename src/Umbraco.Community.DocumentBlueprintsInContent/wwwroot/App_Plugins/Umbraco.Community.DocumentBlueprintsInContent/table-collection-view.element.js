import { UMB_COLLECTION_CONTEXT } from '@umbraco-cms/backoffice/collection';
import {
	UMB_DOCUMENT_BLUEPRINT_ENTITY_TYPE,
	UMB_DOCUMENT_BLUEPRINT_FOLDER_ENTITY_TYPE,
	UMB_EDIT_BLUEPRINT_DOCUMENT_WORKSPACE_PATH_PATTERN,
	UMB_EDIT_DOCUMENT_BLUEPRINT_FOLDER_WORKSPACE_PATH_PATTERN,
} from '@umbraco-cms/backoffice/document-blueprint';
import { css, html } from '@umbraco-cms/backoffice/external/lit';
import { UmbLitElement } from '@umbraco-cms/backoffice/lit-element';
import { UmbPathPattern } from '@umbraco-cms/backoffice/router';
import { UMB_SECTION_CONTEXT } from '@umbraco-cms/backoffice/section';
import { UmbTextStyles } from '@umbraco-cms/backoffice/style';
import { UMB_WORKSPACE_PATH_PATTERN } from '@umbraco-cms/backoffice/workspace';

const TABLE_CONFIG = { allowSelection: false };
const TABLE_COLUMNS = [
	{ name: 'Name', alias: 'name' },
	{ name: '', alias: 'entityActions', align: 'right' },
];

// Core's blueprint edit paths are rooted in the Settings section. This rebuilds the same
// local pattern on top of the given section.
const editPath = (sectionName, entityType, localPattern, unique) =>
	new UmbPathPattern(localPattern.toString(), UMB_WORKSPACE_PATH_PATTERN.generateAbsolute({ sectionName, entityType }))
		.generateAbsolute({ unique });

// Same as core's blueprint children table view, but links to the workspace in the current
// section instead of always to Settings.
class DocumentBlueprintsInContentTableCollectionViewElement extends UmbLitElement {
	static properties = {
		_tableItems: { state: true },
	};

	#items = [];
	#sectionName;

	constructor() {
		super();
		this._tableItems = [];

		this.consumeContext(UMB_SECTION_CONTEXT, (sectionContext) => {
			this.observe(sectionContext?.pathname, (pathname) => {
				this.#sectionName = pathname;
				this.#createTableItems();
			}, 'observeSectionPathname');
		});

		this.consumeContext(UMB_COLLECTION_CONTEXT, (collectionContext) => {
			this.observe(collectionContext?.items, (items) => {
				this.#items = items ?? [];
				this.#createTableItems();
			}, 'observeCollectionItems');
		});
	}

	#createTableItems() {
		// Wait for the section, rather than render links that point at the wrong one.
		if (!this.#sectionName) return;

		this._tableItems = this.#items.map((item) => {
			const href = item.isFolder
				? editPath(this.#sectionName, UMB_DOCUMENT_BLUEPRINT_FOLDER_ENTITY_TYPE, UMB_EDIT_DOCUMENT_BLUEPRINT_FOLDER_WORKSPACE_PATH_PATTERN, item.unique)
				: editPath(this.#sectionName, UMB_DOCUMENT_BLUEPRINT_ENTITY_TYPE, UMB_EDIT_BLUEPRINT_DOCUMENT_WORKSPACE_PATH_PATTERN, item.unique);

			return {
				id: item.unique,
				icon: item.isFolder && !item.icon ? 'icon-folder' : item.icon,
				data: [
					{
						columnAlias: 'name',
						value: html`<uui-button compact href=${href} label=${item.name}></uui-button>`,
					},
					{
						columnAlias: 'entityActions',
						value: html`<umb-entity-actions-table-column-view
							.value=${{ entityType: item.entityType, unique: item.unique, name: item.name }}></umb-entity-actions-table-column-view>`,
					},
				],
			};
		});
	}

	render() {
		return html`<umb-table .config=${TABLE_CONFIG} .columns=${TABLE_COLUMNS} .items=${this._tableItems}></umb-table>`;
	}

	static styles = [
		UmbTextStyles,
		css`
			:host {
				display: flex;
				flex-direction: column;
			}
		`,
	];
}

// Umbraco loads this with a cache-busting query string; any other import of the same file is
// a separate module instance. Define the tag once, and always export the registered class:
// a second, unregistered copy would throw "Illegal constructor" when instantiated.
const TAG = 'document-blueprints-in-content-table-collection-view';
if (!customElements.get(TAG)) {
	customElements.define(TAG, DocumentBlueprintsInContentTableCollectionViewElement);
}

export const element = customElements.get(TAG);
