// for 'malloc' and 'free'
#include <stdlib.h>

// explicit inclusion of 'size_t' and friends (imported by stdlib.h)
#include <stddef.h>

// for 'printf'
#include <stdio.h>

// for 'strcmp', 'strlen', etc.
#include <string.h>

// only needed for calculating exponents. probably will re-implement pow()
// just so Terumi can be compiled on linux
#include <math.h>

// booleans
#include <stdbool.h>

/*
The // <INJECT__CODE> comment signals to the C target to begin translating all
	of the user defined methods at that point.

The // <INJECT__RUN> comment signals to the C target to execute each viable
	main method at that point.
*/

// use this crud only if you're tryna work on this, otherwise leave it
// #define DEBUG
// #define DEBUG_TRACING
#ifdef DEBUG_TRACING
static int level = 0;
void TRACE(const char* place) {
	for (int i = 0; i < level; i++) printf("\t");
	printf("[DEBUG] at '%s'\n", place);
	level++;
}
void TRACE_EXIT(const char* place) {
	level--;
	for (int i = 0; i < level; i++) printf("\t");
	printf("[DEBUG] left '%s'\n", place);
}
#else
void TRACE(const char* place) {
}
void TRACE_EXIT(const char* place) {
}
#endif

// defines how many fields there should be for an object
// NOTICE: DO NOT CHANGE '3' - Terumi will replace '3' with it's own number.
#define GC_OBJECT_FIELDS 3

// if a list runs out of space, it will grow reallocate a new array and copy
// all old elements into the new list.
#define LIST_GROW_MULTIPLIER 3

// defines how big a chunk of memory should be initially.
#define DEFAULT_MEMORY_PAGE_SIZE 4000000

// Metadata for the type of an object in Terumi.
enum ObjectType {

	// 'Unknown' is primarily used to represent newly allocated objects.
	UNKNOWN = 0,

	// 'String' is used to represent objects whose data contains a char*
	STRING = 1,

	// 'Number' is used to represent objects whose data contains a 'struct Number*'
	NUMBER = 2,

	// bool*
	BOOLEAN = 3,

	// struct Object*
	OBJECT = 4,
};

// A 'Value' is used to represent a given value in Terumi. Because Terumi is a
// dynamic language, 'Value' has a generic void* to data, with an 'ObjectType'
// to serve as a signifier for what kind of data is stored. 'Value's are freed
// by the Terumi GC. Do not free them.
struct Value {
	// The type of the value. Useful for determining if it was freshly
	// allocated, or if it is compatible with a call to a given function.
	enum ObjectType type;

	// A pointer to the data of this function. A value must guarentee these
	// kinds of values depending upon the type:
	// UNKNOWN - uninitialized data
	// STRING - char* pointer
	// NUMBER - struct Number* pointer
	// BOOLEAN - bool* pointer
	// OBJECT - struct Object* pointer
	void* data;
};

// A 'ValuePtr' is an index in the GC to a GCEntry, which contains a Value.
// These are used instead of pointers to Value. The GC, on collection, will
// update all 'Value's that are 'Object's with "old value pointers". These
// are particularly useful to gain immediate access to the GCEntry of a value
// in the GC.
struct ValuePtr {
	int object_index;
};

// An 'Object' is used to represent a series of fields, with values. This is
// represented with pointers to values.
struct Object {
	// An array of ValuePtrs to values used by a given object.
	struct ValuePtr fields[GC_OBJECT_FIELDS];
};

// Terumi's number system may expand in the future to floating point operations
// or more. 'Number' is used to encapsulate this. Never access 'data', instead,
// use or create Number specific functions.
struct Number {
	int data;
};

// Helper for void pointer arrays to prevent screwups.
struct VoidPtrArray {
	void** data;
#ifdef DEBUG
	// in debug mode, there is bounds checking
	size_t length;
#endif
};

// Lists are regrowable arrays. If an element is attempted to be added and the
// list is full, a new array will be allocated LIST_GROW_MULTIPLIER times the
// size, and all old data will be copied.
struct List {
	// The inner array of data, represented using a VoidPtrArray
	struct VoidPtrArray array;

	// The total size of the array.
	size_t capacity;

	// The total amount of elements added. This number will constantly be
	// reaching 'capacity', and a fter that, the list will resize.
	int elements;
};

// A GCEntry manages holding a given Value.
struct GCEntry {
	// 'active' only specifies if this object is out of scope of the method it
	// was created in. Once it's not active, it is only kept alive by
	// references to it.
	bool active;

	// 'alive' only specifies if the GCEntry is alive - used as a 'tombstone'.
	// This is particularly useful for the sweep & compact phase of the GC.
	bool alive;

	// A pointer to the value this GCEntry holds.
	struct Value* value;
};

// An instance of the GC. Maintains all GC-related data.
struct GC {
	// A list of struct GCEntry*. All objects that are handheld with gc_handhold
	// are added here.
	struct List list;

	// The threshold of the GC, which is arbitrarily used to determine when to
	// collect.
	size_t threshold;
};

// A large block of memory, which terumi_alloc and terumi_free use.
struct MemoryPage {
	// The size of this memory page.
	size_t size;

	// A raw pointer to the data of this memory page.
	void* data;

	// How many bytes of the memory page has been used. Adding data to used
	// will result in a pointer to where unclaimed data is.
	size_t used;

	// The amount of renters. Every time a consumer allocates some memory, an
	// appropriate memory block is found and the rented number is incremented.
	// Once the user returns that block, the rented number is decremented, and
	// if this block has no renters, 'used' will be reset upon the next alloc.
	int rented;
};

// An instance of Memory. Maintains all Memory-related data.
struct Memory {
	// The size of a page. Will increase if lots of data is needed.
	size_t page_size;

	// A list of struct Memory*. This maintains all the memory chunks.
	struct List memory_pages;
};

// Global instance of the Memory. Used for the terumi allocators.
struct Memory memory;
bool memory_init = false;

// Ensures the Memory is initialized.
void ensure_memory_init();
// Allocates some data with terumi's memory paging system.
void* terumi_alloc(size_t data);
// Frees some data with terumi's memory paging system.
bool terumi_free(void* data);
// If 'terumi_free' fails, this will print a message to standard out for you.
void terumi_free_warn(void* data);

// Global instance of the GC. Used for managing 'struct Value's.
struct GC gc;
bool gc_init = false;

// Ensures the GC is initialized.
void ensure_gc_init();
// Runs the GC. Returns the amount of objects collected.
int run_gc();
// Returns 'true' if the GC should be ran (some threshold was passed)
bool should_run_gc();
// Might run the GC. Returns -1 if it didn't, otherwise it returns the amount
// of objects the GC collected.
int maybe_run_gc();
// registers a Value* to be managed (aka, handheld) by the GC. The caller is
// expected to set 'active' to 'false' on the returned GC entry once they are
// no longer going to use the value. Values must be created with the value
// allocator functions. The caller should NOT call terumi_free, as the GC will.
struct GCEntry* gc_handhold(struct Value* value);

// Creates a new void pointer array with a given size.
struct VoidPtrArray* voidptrarray_new(size_t capacity);
// Resizes a void pointer array to a new size. The old size is used in debug
// mode for array bound checks.
void voidptrarray_regrow(struct VoidPtrArray* data, size_t new_size, size_t old_size);
// Gets an element at a given index of a struct VoidPtrArray*.
void* voidptrarray_at(struct VoidPtrArray* array, int index);
// Sets an element at a given index of a struct VoidPtrArray*.
void voidptrarray_set(struct VoidPtrArray* array, int index, void* value);

// Constructs a number from an integer.
struct Number* number_from_int(int integer);
// Adds two numbers together.
struct Number* number_add(struct Number* left, struct Number* right);

// Constructs a Value from a string. Uses the terumi allocator.
struct Value* value_from_string(char* string);
// Constructs a Value from a boolean. Reuses a global Value*. Do not mark as
// inactive.
struct Value* value_from_boolean(bool state);
// Constructs a Value from an Object. Uses the terumi allocator.
struct Value* value_from_object(struct Object* object);
// Constructs a Value from a Number. Uses the terumi allocator.
struct Value* value_from_number(struct Number* number);

// Constructs a new List with an initial capacity.
struct List* list_new(size_t initial_capacity);
// Appends an item to a given list, and resizes if necessary.
int list_add(struct List* list, void* item);

#pragma region VoidPtrArray
struct VoidPtrArray* voidptrarray_new(size_t capacity) {
	TRACE("voidptrarray_new");
	struct VoidPtrArray* array = malloc(sizeof(struct VoidPtrArray));
	void** data = malloc(sizeof(void*) * capacity);
	array->data = data;
#ifdef DEBUG
	array->length = capacity;
#endif
	TRACE_EXIT("voidptrarray_new");
	return array;
}

void voidptrarray_regrow(struct VoidPtrArray* data, size_t new_size, size_t old_size) {
	TRACE("voidptrarray_regrow");
	if (data->data == NULL) {
		printf("data NULL");
	}

	// TODO: use realloc
	void** new = malloc(sizeof(void*) * new_size);
	// memcpy doesn't seem to work...?
	for (int i = 0; i < old_size; i++) {
		new[i] = data->data[i];
	}
	free(data->data);
	data->data = new;
#ifdef DEBUG
	data->length = new_size;
#endif

	TRACE_EXIT("voidptrarray_regrow");
}

void* voidptrarray_at(struct VoidPtrArray* data, int index) {
	TRACE("voidptrarray_at");
#ifdef DEBUG
	if (index >= data->length) {
		printf("[WARN] OUT OF BOUNDS ACCESS ON VOID POINTER ARRAY: accesing '%i' of length '%i'\n", index, data->length);
	}
#endif
	void* voidptr = data->data[index];
	TRACE_EXIT("voidptrarray_at");
	return voidptr;
}

void voidptrarray_set(struct VoidPtrArray* data, int index, void* value) {
	TRACE("voidptrarray_set");
#ifdef DEBUG
	if (index >= data->length) {
		printf("[WARN] OUT OF BOUNDS ACCESS ON VOID POINTER ARRAY: accesing '%i' of length '%i'\n", index, data->length);
	}
#endif
	data->data[index] = value;
	TRACE_EXIT("voidptrarray_set");
}
#pragma endregion

#pragma region GC
void ensure_gc_init() {
	TRACE("ensure_gc_init");
	if (!gc_init) {
		gc.list = *list_new(102400);
		gc.threshold = 102400;
		gc_init = true;
	}
	TRACE_EXIT("ensure_gc_init");
}

int maybe_run_gc() {
	TRACE("maybe_run_gc");
	ensure_gc_init();
	int result = should_run_gc() ? run_gc() : -1;

	// GC NOTICE:
	// if we remove more than 60% of the objects, we will downsize our threshold by half
	// if we don't remove     ^^^ of the objects, we will increase our threshold by 2x

	float percent_60 = (float)result / (float)gc.threshold;

	if (percent_60 > 0.60) {
		// gc.threshold /= 2;
	}
	else {
		// gc.threshold *= 2;
	}

	TRACE_EXIT("maybe_run_gc");
	return result;
}

void cleanup_value(struct Value* value) {
	// TODO: figure out best wayt to clear each one
	switch (value->type) {
	case STRING: {
		// free(value->data);
	} break;
	case NUMBER: {

	} break;
	}
}

bool should_run_gc() {
	TRACE("should_run_gc");
	ensure_gc_init();
	bool should = gc.list.elements >= gc.threshold;
	TRACE_EXIT("should_run_gc");
	return should;
}

bool mark_referenced(bool* referenced) {
	TRACE("mark_referenced");
	bool modified = false;

	for (int i = 0; i < gc.list.elements; i++) {
		struct GCEntry* gcentry = voidptrarray_at(&gc.list.array, i);

		// active items are referenced in a method
		if (gcentry->active
			// that aren't already marked
			&& !referenced[i]) {

			referenced[i] = true;
			modified = true;
		}

		// if it's not referenced, we don't want to mark anything it references
		// as referenceable.
		if (!referenced[i]) continue;

		// has to be an object so we can mark stuff as referenceable
		struct Value* value = gcentry->value;
		if (value->type != OBJECT) continue;

		struct Object* object = value->data;

		// mark each object the referenced object references as referenced.
		for (int j = 0; j < GC_OBJECT_FIELDS; j++) {

			int index = object->fields[j].object_index;

			if (!referenced[index]) {
				modified = true;
				referenced[index] = true;
			}
		}
	}

	TRACE_EXIT("mark_referenced");
	return modified;
}

void swap_gc(int from, int to) {
	TRACE("swap_gc");
	for (int i = 0; i < gc.list.elements; i++) {
		struct GCEntry* datai = voidptrarray_at(&gc.list.array, i);
		if (!datai->alive) continue;
		if (datai->value->type != OBJECT) continue;

		struct Object* obj = datai->value->data;
		for (int j = 0; j < GC_OBJECT_FIELDS; j++) {
			if (obj->fields[j].object_index == from) {
				obj->fields[j].object_index = to;
			}
		}
	}
	TRACE_EXIT("swap_gc");
}

void compact_gc() {
	TRACE("compact_gc");
	int begin = 0;
	int end = gc.list.elements - 1;
	int last_end = gc.list.elements + 1;

	// to compact the GC, we'll swap alive items from the back to dead spaces
	// in the front.
	while (true) {

		// find a dead item in the front
		for (; begin < gc.list.elements; begin++) {
			struct GCEntry* entry = voidptrarray_at(&gc.list.array, begin);

			if (!(entry->alive)) {
				break;
			}
		}

		// find an alive item from the back
		for (; end >= 0; end--) {
			struct GCEntry* entry = voidptrarray_at(&gc.list.array, end);

			if (entry->alive) {
				break;
			}
		}

		if (end == -1) {
			// there isn't a single alive item
			break;
		}

		// if the begin is in front of the end, don't swap
		if (begin >= end) break;

		swap_gc(end, begin);
		last_end = end;
	}

	if (end != -1 && begin >= end) {
		TRACE_EXIT("compact_gc");
		return;
	}

	// free all dead elements and resize list
	// if there were no alive elements, free the entire list
	if (end == -1) last_end = 0;

	for (int i = last_end; i < gc.list.elements; i++) {
		struct GCEntry* entry = voidptrarray_at(&gc.list.array, i);
		terumi_free_warn(entry);
	}
	gc.list.elements = last_end;

	TRACE_EXIT("compact_gc");
}

int run_gc() {
	TRACE("run_gc");
	ensure_gc_init();
	// MARK
	bool* referenced = terumi_alloc(sizeof(bool) * gc.list.elements);
	memset(referenced, false, sizeof(bool) * gc.list.elements);

	// this will mark all objects referenced by an alive object as alive.
	while (mark_referenced(referenced)) {
	}

	// SWEEP
	int cleaned = 0;
	for (int i = 0; i < gc.list.elements; i++) {
		if (!referenced[i]) {
			struct GCEntry* entry = voidptrarray_at(&gc.list.array, i);

			if (entry->alive) {
				cleaned++;
				entry->active = false;
				entry->alive = false;
				cleanup_value(entry->value);
				terumi_free_warn(entry->value);
			}
		}
	}

	if (cleaned > 0) {
		compact_gc();
	}

	terumi_free_warn(referenced);
	TRACE_EXIT("run_gc");
	return cleaned;
}

struct GCEntry* gc_handhold(struct Value* value) {
	TRACE("gc_handhold");
	ensure_gc_init();

	struct GCEntry* entry = terumi_alloc(sizeof(struct GCEntry));
	entry->alive = true;
	entry->active = true;
	entry->value = value;
	list_add(&gc.list, entry);
	TRACE_EXIT("gc_handhold");
	return entry;
}
#pragma endregion

#pragma region List
int list_add(struct List* list, void* item) {
	TRACE("list_add");
	// if the list has reached maximum capacity
	if (list->elements >= list->capacity) {

		// double the capacity
		size_t new_capacity = list->capacity * LIST_GROW_MULTIPLIER;
		voidptrarray_regrow(&list->array, new_capacity, list->capacity);
		list->capacity = new_capacity;
	}

	// insert the item into the array
	int index = list->elements++;
	voidptrarray_set(&list->array, index, item);
	TRACE_EXIT("list_add");
	return index;
}

// the memory paging isn't setup and needs a list
// so it calls the malloc version
struct List* list_new(size_t initial_capacity) {
	TRACE("list_new_malloc");
	struct List* list = malloc(sizeof(struct List));
	list->capacity = initial_capacity;
	list->elements = 0;
	list->array = *voidptrarray_new(initial_capacity);
	TRACE_EXIT("list_new_malloc");
	return list;
}
#pragma endregion

#pragma region Memory
/* Memory Paging */
// memory paging creates pages of memory and allocates and deallocates from the
// pages, instead of individual malloc calls. That way, fewer mallocs are
// performed.
size_t terumi_resize_page_size(size_t initial, size_t target) {
	TRACE("terumi_resize_page_size");
	size_t size = initial;

	while (size < target) {
		size *= 2;
	}

	TRACE_EXIT("terumi_resize_page_size");
	return size;
}

// News up a page - used and rented are left uninitialized for the caller to
// set.
struct MemoryPage* terumi_new_page(size_t page_size) {
	TRACE("terumi_new_page");
	void* data = malloc(page_size);
	struct MemoryPage* page = malloc(sizeof(struct MemoryPage));
	page->size = page_size;
	page->data = data;
	// page->used = 0;
	// page->rented = 0;
	list_add(&memory.memory_pages, page);
	TRACE_EXIT("terumi_new_page");
	return page;
}

void* terumi_alloc(size_t data) {
	TRACE("terumi_alloc");
	if (!memory_init) {
		memory.memory_pages = *list_new(4);
		memory.page_size = DEFAULT_MEMORY_PAGE_SIZE;
		memory_init = true;
	}

	if (memory.page_size < data) {
		// if the data is bigger than a page, we are guarenteed to not have any
		// pages open. resize the page, then allocate a new page.

		memory.page_size = terumi_resize_page_size(memory.page_size, data);
		struct MemoryPage* page = terumi_new_page(memory.page_size);

		page->rented = 1;
		page->used = data;
		void* page_data = page->data;
		TRACE_EXIT("terumi_alloc");
		return page_data;
	}

	// look through all available pages, and see if there's enough to rent for
	// the caller
	for (int i = 0; i < memory.memory_pages.elements; i++) {
		struct MemoryPage* page = voidptrarray_at(&memory.memory_pages.array, i);

		if (page->rented == 0) {
			// if this is an empty page, we can allocate here.
			page->used = data;
			page->rented = 1;
			void* page_data = page->data;
			TRACE_EXIT("terumi_alloc");
			return page_data;
		}

		if (data + page->used < page->size) {
			// there's enough space in this page to allocate here
			void* voidptr = page->data + page->used;
			page->used += data;
			page->rented++;
			TRACE_EXIT("terumi_alloc");
			return voidptr;
		}
	}

	// there are no pages big enough for the caller. we'll make a new page.
	for (int i = 0; i < memory.memory_pages.elements; i++) {
		struct MemoryPage* page = voidptrarray_at(&memory.memory_pages.array, i);
	}

	struct MemoryPage* page = terumi_new_page(memory.page_size);
	page->used = data;
	page->rented = 1;
	void* page_data = page->data;
	TRACE_EXIT("terumi_alloc");
	return page_data;
}

bool terumi_free(void* data) {
	TRACE("terumi_free");
	if (!memory_init) {
		printf("[PANIC] attempted to free data and memory isn't initialized.\n");
		TRACE_EXIT("terumi_free");
		return false;
	}

	// first, let's find the page the pointer is in
	for (int i = 0; i < memory.memory_pages.elements; i++) {
		struct MemoryPage* page = voidptrarray_at(&memory.memory_pages.array, i);

		// if the pointer is within the block of memory
		if ((data >= page->data) && (data < (page->data + page->size))) {

			// there's one less renter.
			page->rented--;
#ifdef DEBUG
			if (page->rented < 0) {
				printf("[WARN] page->rented < 0");
			}
#endif
			TRACE_EXIT("terumi_free");
			return true;
		}
	}

	TRACE_EXIT("terumi_free");
	return false;
}

void terumi_free_warn(void* x) {
	TRACE("terumi_free_warn");
	if (!terumi_free(x)) {
		//printf("[WARN] Unable to free data at '%i'\n", (int)x);
	}
	TRACE_EXIT("terumi_free_warn");
}
#pragma endregion

#pragma region Value
struct Value* value_from_string(char* string) {
	TRACE("value_from_string");
	struct Value* value = terumi_alloc(sizeof(struct Value));
	value->type = STRING;
	value->data = string;
	TRACE_EXIT("value_from_string");
	return value;
}
#pragma endregion

#pragma region TerumiCompilerMethods
// <INJECT__CODE>
#pragma endregion

int main(int argc, char** argv) {
	TRACE("main");

	for (int i = 0; i < 100000000; i++) {
		struct GCEntry* entry = gc_handhold(value_from_string("ok boomer"));
		entry->active = false;
		maybe_run_gc();
	}

	// <INJECT__RUN>

	// politely kill gc
	for (int i = 0; i < gc.list.elements; i++) {
		struct GCEntry* entry = voidptrarray_at(&gc.list.array, i);
		entry->active = false;
	}

	printf("Terumi cleanup: %i objects cleaned\n", run_gc());

	// free all pages
	for (int i = 0; i < memory.memory_pages.elements; i++) {
		free(voidptrarray_at(&memory.memory_pages.array, i));
	}

	TRACE_EXIT("main");
	return 0;
}