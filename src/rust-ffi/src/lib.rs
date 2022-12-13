#![allow(clippy::missing_safety_doc)]
#![allow(clippy::not_unsafe_ptr_arg_deref)]
use std::{slice, ptr};

// Interop code
use bulletproofs::PedersenGens;
use curve25519_dalek_ng::{ristretto::{RistrettoPoint, CompressedRistretto}, scalar::Scalar};

#[no_mangle]
pub extern "C" fn pedersen_gens_default() -> *mut PedersenGens {
    Box::into_raw(Box::default())
}

#[no_mangle]
pub unsafe extern "C" fn pedersen_gens_new(
    g: *const RistrettoPoint,
    h: *const RistrettoPoint,
) -> *mut PedersenGens {
    let g = *g;
    let h = *h; // consider if Rust steals these Points?
    Box::into_raw(Box::new(PedersenGens { B: g, B_blinding: h }))
}

#[no_mangle]
pub unsafe extern "C" fn pedersen_gens_commit(
    this: *mut PedersenGens,
    value: *const u8,
    blinding: *const u8,
) -> *mut RistrettoPoint {
    let this = &*this;
    let value = slice::from_raw_parts(value, 32);
    let blinding = slice::from_raw_parts(blinding, 32);

    let value = Scalar::from_bytes_mod_order(value.try_into().unwrap());
    let blinding = Scalar::from_bytes_mod_order(blinding.try_into().unwrap());

    Box::into_raw(Box::new(this.commit(value, blinding)))
}


#[no_mangle]
pub extern "C" fn pedersen_gens_free(this: *mut PedersenGens) {
    if this.is_null() {
        return;
    }
    unsafe {
        drop(Box::from_raw(this));
    }
}

#[no_mangle]
pub unsafe extern "C" fn ristretto_point_from_uniform_bytes(bytes: *const u8) -> *const RistrettoPoint {
    let bytes = slice::from_raw_parts(bytes, 64).try_into().unwrap();
    Box::into_raw(Box::new(RistrettoPoint::from_uniform_bytes(bytes)))
}


#[no_mangle]
pub unsafe extern "C" fn ristretto_point_compress(
    this: *const RistrettoPoint,
    dst: *mut u8,
) {
    let this = &*this;
    let dst = slice::from_raw_parts_mut(dst, 32);
    let src = this.compress().to_bytes();
    dst.clone_from_slice(&src);
}


#[no_mangle]
pub extern "C" fn ristretto_point_decompress(
    bytes: *const u8,
) -> *const RistrettoPoint {
    let bytes = unsafe{slice::from_raw_parts(bytes, 32)};
    let compressed = CompressedRistretto::from_slice(bytes);
    let Some(point) = compressed.decompress() else {
        return ptr::null(); // follow C-style error convetion
    };
    Box::into_raw(Box::new(point))
}


#[no_mangle]
pub unsafe extern "C" fn ristretto_point_equals(
    lhs: *const RistrettoPoint,
    rhs: *const RistrettoPoint,
) -> *const RistrettoPoint {
    let lhs = &*lhs;
    let rhs = &*rhs;
    Box::into_raw(Box::new(lhs + rhs))
}

#[no_mangle]
pub unsafe extern "C" fn ristretto_point_add(
    lhs: *const RistrettoPoint,
    rhs: *const RistrettoPoint,
) -> bool {
    let lhs = &*lhs;
    let rhs = &*rhs;
    lhs == rhs
}


#[no_mangle]
pub unsafe extern "C" fn ristretto_point_mul_scalar(
    lhs: *const RistrettoPoint,
    rhs: *const u8,
) -> *const RistrettoPoint {
    let lhs = &*lhs;

    let rhs = slice::from_raw_parts(rhs, 32);
    let rhs = Scalar::from_bytes_mod_order(rhs.try_into().unwrap());
    Box::into_raw(Box::new(lhs * rhs))
}

#[no_mangle]
pub extern "C" fn ristretto_point_free(this: *mut RistrettoPoint) {
    if this.is_null() {
        return;
    }
    unsafe {
        drop(Box::from_raw(this));
    }
}
